using System;
using System.Threading;
using System.Threading.Tasks;
using FPT.Communication;
using FPT.Core;
using RosSharp.RosBridgeClient.MessageTypes.FptInterface;
using RosSharp.RosBridgeClient.MessageTypes.Geometry;
using RosSharp.RosBridgeClient.MessageTypes.Sensor;

using UnityEngine;

namespace FPT.Business
{
    /// <summary> 控制模式：由最后一次用户输入决定 </summary>
    public enum ControlMode
    {
        JointSpace,     // 用户最后操作了关节滑块
        CartesianSpace  // 用户最后操作了末端位姿
    }

    /// <summary>
    /// 输入终端 — 数据同步中枢
    /// 保持 JointAngles ↔ EndEffectorPose 双向互通（通过 ROS2 Service）
    /// GhostArm 只读 JointAngles，UI 订阅事件同步回显
    /// </summary>
    public class InputTerminal
    {
        // ═══ 关节限制（度，仅 arm1_link1~6，不含 plate_joint） ═══
        public static readonly double[] ArmJointMinDeg = { -178, -178, -178, -178, -178, -180 };
        public static readonly double[] ArmJointMaxDeg = {  178,  178,  145,  178,  178,  180 };

        private readonly Ros2Node _node;

        // ═══ 数据 ═══
        public double[] JointAngles { get; private set; }        // 7 DOF（度）
        public DevicePose EndEffectorPose { get; private set; }  // 相对 ReferenceFrame
        public string ReferenceFrame { get; private set; } = "base_link";
        public ControlMode ActiveMode { get; private set; } = ControlMode.JointSpace;
        public bool IsPlanning { get; private set; }  // 正在等待 FK/IK 响应
        public bool PlanReady { get; private set; }   // 笛卡尔规划成功，可以确认执行

        // ═══ 事件（UI / GhostArm 订阅） ═══
        public event Action<double[]> OnJointAnglesChanged;   // GhostArm 驱动（7 个角度，度）
        public event Action<DevicePose> OnEePoseChanged;      // UI 末端位姿回显
        public event Action<string> OnStatusChanged;          // 状态消息

        // ═══ 防循环 + 去抖 ═══
        private bool _jointDirty;        // 用户改了关节角，等待 FK 返回
        private bool _poseDirty;         // 用户改了末端位姿，等待 IK 返回
        private CancellationTokenSource _debounceCts;
        private const float DebounceMs = 300f;

        public InputTerminal(Ros2Node node)
        {
            _node = node;
            JointAngles = new double[7];
            EndEffectorPose = DevicePose.Identity;

            // 订阅 /plan_status topic
            _node.Subscribe<PlanStatus>("/plan_status", msg =>
            {
                OnPlanStatus(msg.status);
            });
        }

        // ═══════════════════════════════════════════
        // 输入（UI 调用）
        // ═══════════════════════════════════════════

        /// <summary> 用户改变关节滑块 → 请求 FK </summary>
        public void SetJointAngles(double[] angles)
        {
            if (angles == null || angles.Length < 7) return;

            // clamp arm joints 1-6（索引 1-6），plate_joint（索引 0）不限
            angles[0] = Math.Max(-360, Math.Min(360, angles[0])); // 转台宽松限制
            for (int i = 0; i < 6; i++)
                angles[i + 1] = ClampArmJoint(i, angles[i + 1]);

            JointAngles = angles;
            ActiveMode = ControlMode.JointSpace;
            PlanReady = false;

            _jointDirty = true;
            _poseDirty = false;
            NotifyJointAnglesChanged();
            RequestFkDebounced();
        }

        /// <summary> clamp 单个臂关节角 </summary>
        public static double ClampArmJoint(int index, double value)
        {
            if (index < 0 || index >= 6) return value;
            return Math.Max(ArmJointMinDeg[index], Math.Min(ArmJointMaxDeg[index], value));
        }

        /// <summary> 用户改变末端位姿 → 请求 IK </summary>
        public void SetEndEffectorPose(DevicePose pose)
        {
            EndEffectorPose = pose;
            ActiveMode = ControlMode.CartesianSpace;
            PlanReady = false;

            _poseDirty = true;
            _jointDirty = false;
            NotifyEePoseChanged();
            RequestIkDebounced();
        }

        /// <summary> 用户切换参考坐标系 → 如有目标位姿则重新请求 IK </summary>
        public void SetReferenceFrame(string frameId)
        {
            if (string.IsNullOrEmpty(frameId) || frameId == ReferenceFrame) return;
            ReferenceFrame = frameId;

            if (ActiveMode == ControlMode.CartesianSpace && !_poseDirty)
            {
                _poseDirty = true;
                RequestIkDebounced();
            }
        }

        // ═══════════════════════════════════════════
        // 执行
        // ═══════════════════════════════════════════

        /// <summary> 确认执行 — 根据 ActiveMode 调用不同 service </summary>
        public async void ConfirmExecute()
        {
            Debug.Log($"[InputTerminal] ConfirmExecute: mode={ActiveMode}, PlanReady={PlanReady}");

            if (ActiveMode == ControlMode.JointSpace)
            {
                try
                {
                    // Step 1: joint_plan — 规划轨迹
                    var jointMsg = JointTrajectoryMapper.CreateJointState(JointAngles, ReferenceFrame);
                    var jointReq = new JointPlanRequest(jointMsg);
                    await _node.CallServiceAsync<JointPlanResponse>("joint_plan", jointReq);
                    Debug.Log("[InputTerminal] joint_plan 完成，开始执行...");

                    // Step 2: execute — 执行轨迹
                    var execReq = new ExecuteRequest(true);
                    var execResp = await _node.CallServiceAsync<ExecuteResponse>("execute", execReq);
                    Debug.Log($"[InputTerminal] execute 结果: success={execResp.success}, msg={execResp.message}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[InputTerminal] 执行失败: {ex.Message}");
                    OnStatusChanged?.Invoke("failed");
                }
            }
            else
            {
                if (!PlanReady)
                {
                    Debug.LogWarning("[InputTerminal] 笛卡尔规划尚未就绪，无法执行");
                    OnStatusChanged?.Invoke("failed");
                    return;
                }
                // 调用 execute service
                try
                {
                    var execReq = new ExecuteRequest(true);
                    var execResp = await _node.CallServiceAsync<ExecuteResponse>("execute", execReq);
                    Debug.Log($"[InputTerminal] execute service 结果: success={execResp.success}, msg={execResp.message}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[InputTerminal] execute service 调用失败: {ex.Message}");
                    OnStatusChanged?.Invoke("failed");
                }
            }
            PlanReady = false;
            IsPlanning = false;
        }

        /// <summary> 取消当前操作 </summary>
        public void Cancel()
        {
            _debounceCts?.Cancel();
            _jointDirty = false;
            _poseDirty = false;
            PlanReady = false;
            IsPlanning = false;
            OnStatusChanged?.Invoke("cancelled");
            Debug.Log("[InputTerminal] 操作已取消");
        }

        // ═══════════════════════════════════════════
        // ROS 回调（内部）
        // ═══════════════════════════════════════════

        private void OnPlanStatus(string status)
        {
            switch (status)
            {
                case "planning":
                    IsPlanning = true;
                    break;
                case "success":
                    IsPlanning = false;
                    break;
                case "failed":
                    IsPlanning = false;
                    PlanReady = false;
                    _poseDirty = false;
                    break;
                case "executed":
                    PlanReady = false;
                    break;
            }
            OnStatusChanged?.Invoke(status);
        }

        // ═══════════════════════════════════════════
        // 去抖 + Service 调用
        // ═══════════════════════════════════════════

        private async void RequestFkDebounced()
        {
            try
            {
                _debounceCts?.Cancel();
                _debounceCts = new CancellationTokenSource();
                var token = _debounceCts.Token;
                await Task.Delay((int)DebounceMs, token);
                if (!token.IsCancellationRequested)
                {
                    IsPlanning = true;
                    var jointMsg = JointTrajectoryMapper.CreateJointState(JointAngles, ReferenceFrame);
                    var jointReq = new JointPlanRequest(jointMsg);
                    var result = await _node.CallServiceAsync<JointPlanResponse>("joint_plan", jointReq);

                    if (!_jointDirty) return;
                    _jointDirty = false;
                    IsPlanning = false;

                    EndEffectorPose = JointTrajectoryMapper.ToDevicePose(result.response);
                    NotifyEePoseChanged();
                    Debug.Log($"[InputTerminal] FK 结果: {EndEffectorPose}");
                }
            }
            catch (TaskCanceledException) { /* 被新的去抖取消，正常 */ }
            catch (Exception ex)
            {
                IsPlanning = false;
                Debug.LogError($"[InputTerminal] FK service 调用失败: {ex.Message}");
            }
        }

        private async void RequestIkDebounced()
        {
            try
            {
                _debounceCts?.Cancel();
                _debounceCts = new CancellationTokenSource();
                var token = _debounceCts.Token;
                await Task.Delay((int)DebounceMs, token);
                if (!token.IsCancellationRequested)
                {
                    IsPlanning = true;
                    var poseMsg = JointTrajectoryMapper.CreatePoseStamped(EndEffectorPose, ReferenceFrame);
                    var eeReq = new EEPlanRequest(poseMsg);
                    var result = await _node.CallServiceAsync<EEPlanResponse>("ee_plan", eeReq);

                    if (!_poseDirty) return;
                    _poseDirty = false;
                    IsPlanning = false;
                    PlanReady = true;

                    JointAngles = JointTrajectoryMapper.ToJointAnglesDeg(result.response);
                    NotifyJointAnglesChanged();
                    Debug.Log($"[InputTerminal] IK 结果: [{string.Join(", ", JointAngles)}]");
                }
            }
            catch (TaskCanceledException) { /* 被新的去抖取消，正常 */ }
            catch (Exception ex)
            {
                IsPlanning = false;
                Debug.LogError($"[InputTerminal] IK service 调用失败: {ex.Message}");
            }
        }

        private void NotifyJointAnglesChanged()
        {
            OnJointAnglesChanged?.Invoke(JointAngles);
        }

        private void NotifyEePoseChanged()
        {
            OnEePoseChanged?.Invoke(EndEffectorPose);
        }

        /// <summary> 用实时关节状态初始化输入终端（启动时或取消后） </summary>
        public void SyncFromRealState(double[] realAngles)
        {
            if (realAngles == null || realAngles.Length < 7) return;
            JointAngles = realAngles;
            _jointDirty = false;
            _poseDirty = false;
            PlanReady = false;
            IsPlanning = false;
            ActiveMode = ControlMode.JointSpace;
            NotifyJointAnglesChanged();
        }

        public void Dispose()
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
        }
    }
}
