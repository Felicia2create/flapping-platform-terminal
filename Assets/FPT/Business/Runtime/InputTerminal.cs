using System;
using System.Threading;
using System.Threading.Tasks;
using FPT.Communication;
using FPT.Core;
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
    /// 保持 JointAngles ↔ EndEffectorPose 双向互通（通过 ROS2 FK/IK）
    /// GhostArm 只读 JointAngles，UI 订阅事件同步回显
    /// </summary>
    public class InputTerminal
    {
        // ═══ 关节限制（度，仅 arm1_link1~6，不含 plate_joint） ═══
        public static readonly double[] ArmJointMinDeg = { -178, -178, -178, -178, -178, -180 };
        public static readonly double[] ArmJointMaxDeg = {  178,  178,  145,  178,  178,  180 };

        private readonly Ros2PlanningBridge _ros2;

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

        public InputTerminal(Ros2PlanningBridge ros2)
        {
            _ros2 = ros2;
            JointAngles = new double[7];
            EndEffectorPose = DevicePose.Identity;

            _ros2.OnFkResult += OnFkResult;
            _ros2.OnIkResult += OnIkResult;
            _ros2.OnPlanStatus += OnPlanStatus;
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

            // 如果在笛卡尔模式下已有目标，重新触发 IK（用新坐标系）
            if (ActiveMode == ControlMode.CartesianSpace && !_poseDirty)
            {
                _poseDirty = true;
                RequestIkDebounced();
            }
        }

        // ═══════════════════════════════════════════
        // 执行
        // ═══════════════════════════════════════════

        /// <summary> 确认执行 — 根据 ActiveMode 发不同话题 </summary>
        public void ConfirmExecute()
        {
            Debug.Log($"[InputTerminal] ConfirmExecute: mode={ActiveMode}, PlanReady={PlanReady}, JointAngles=[{string.Join(", ", JointAngles)}]");

            if (ActiveMode == ControlMode.JointSpace)
            {
                _ros2.PublishJointCommand(JointAngles);
                Debug.Log($"[InputTerminal] 关节空间确认: /joint_commands 已发送");
            }
            else
            {
                if (!PlanReady)
                {
                    Debug.LogWarning("[InputTerminal] 笛卡尔规划尚未就绪，无法执行 /execute");
                    OnStatusChanged?.Invoke("failed");
                    return;
                }
                _ros2.PublishExecute();
                Debug.Log("[InputTerminal] 笛卡尔空间确认: /execute 已发送");
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

        private void OnFkResult(DevicePose pose)
        {
            if (!_jointDirty) return;  // 不是等 FK，忽略
            _jointDirty = false;
            IsPlanning = false;

            EndEffectorPose = pose;
            NotifyEePoseChanged();  // 更新 UI EE 字段（不触发 IK）
            Debug.Log($"[InputTerminal] FK 结果: {pose}");
        }

        private void OnIkResult(double[] angles)
        {
            if (!_poseDirty) return;  // 不是等 IK，忽略
            _poseDirty = false;
            IsPlanning = false;
            PlanReady = true;

            JointAngles = angles;
            NotifyJointAnglesChanged();  // GhostArm 更新（不触发 FK）
            Debug.Log($"[InputTerminal] IK 结果: [{string.Join(", ", angles)}]");
        }

        private void OnPlanStatus(string status)
        {
            switch (status)
            {
                case "planning":
                    IsPlanning = true;
                    break;
                case "success":
                    IsPlanning = false;
                    // PlanReady 在 OnIkResult 中设置
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
        // 去抖 + 通知
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
                    _ros2.PublishFkRequest(JointAngles, ReferenceFrame);
                }
            }
            catch (TaskCanceledException) { /* 被新的去抖取消，正常 */ }
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
                    _ros2.PublishIkRequest(EndEffectorPose, ReferenceFrame);
                }
            }
            catch (TaskCanceledException) { /* 被新的去抖取消，正常 */ }
        }

        private void NotifyJointAnglesChanged()
        {
            // GHOSTARM SUBSCRIBES HERE — fires only once per change
            OnJointAnglesChanged?.Invoke(JointAngles);
        }

        private void NotifyEePoseChanged()
        {
            // 末端位姿显示订阅（不回触发 IK）
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
            // 不请求 FK — 让用户主动拖动滑块时再触发
        }

        public void Dispose()
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            if (_ros2 != null)
            {
                _ros2.OnFkResult -= OnFkResult;
                _ros2.OnIkResult -= OnIkResult;
                _ros2.OnPlanStatus -= OnPlanStatus;
            }
        }
    }
}
