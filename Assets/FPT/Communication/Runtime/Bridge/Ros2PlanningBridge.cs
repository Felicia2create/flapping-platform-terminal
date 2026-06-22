using System;
using RosMessageTypes.Geometry;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using FPT.Core;
using UnityEngine;

namespace FPT.Communication
{
    /// <summary>
    /// 规划相关 ROS2 通信代理
    /// 组合 Ros2Bridge，封装 FK / IK / Execute 5 个话题的收发
    /// 弧度↔度转换在此层完成，对外全部使用度
    /// </summary>
    public class Ros2PlanningBridge
    {
        private readonly Ros2Bridge _ros2;
        private readonly string _defaultFrameId;

        // —— 事件（ROS 回调 → 业务层） ——
        public event Action<DevicePose> OnFkResult;
        public event Action<double[]> OnIkResult;       // 关节角（度），7 DOF
        public event Action<string> OnPlanStatus;

        public bool IsConnected => _ros2 != null && _ros2.IsConnected;

        public Ros2PlanningBridge(Ros2Bridge ros2, string defaultFrameId = "base_link")
        {
            _ros2 = ros2;
            _defaultFrameId = defaultFrameId;

            // 注册纯发布话题（无订阅端的话题必须注册后才能 Publish）
            _ros2.RegisterPublisher<JointStateMsg>("/compute_fk");
            _ros2.RegisterPublisher<PoseStampedMsg>("/compute_ik");
            _ros2.RegisterPublisher<JointStateMsg>("/joint_commands");
            _ros2.RegisterPublisher<RosMessageTypes.Std.BoolMsg>("/execute");

            SubscribeAll();
        }

        // ═══════════════════════════════════════════════
        // 发布（ROS 未连接时静默跳过）
        // ═══════════════════════════════════════════════

        /// <summary> 发布 FK 请求 → /compute_fk（7 DOF，含 frame_id） </summary>
        public void PublishFkRequest(double[] anglesDeg, string frameId = null)
        {
            if (!IsConnected) return;
            var msg = JointTrajectoryMapper.CreateJointState(anglesDeg, frameId ?? _defaultFrameId);
            _ros2.Publish("/compute_fk", msg);
        }

        /// <summary> 发布 IK 请求 → /compute_ik（Cartesian 目标，含 frame_id） </summary>
        public void PublishIkRequest(DevicePose targetPose, string frameId = null)
        {
            if (!IsConnected) return;
            var msg = JointTrajectoryMapper.CreatePoseStamped(targetPose, frameId ?? _defaultFrameId);
            _ros2.Publish("/compute_ik", msg);
        }

        /// <summary> 发布关节指令 → /joint_commands（7 DOF，确认执行用） </summary>
        public void PublishJointCommand(double[] anglesDeg)
        {
            if (!IsConnected) return;
            var msg = JointTrajectoryMapper.CreateJointState(anglesDeg, _defaultFrameId);
            _ros2.Publish("/joint_commands", msg);
        }

        /// <summary> 发布执行信号 → /execute (BoolMsg) </summary>
        public void PublishExecute()
        {
            if (!IsConnected) { Debug.LogWarning("[Ros2PlanningBridge] ROS 未连接，/execute 跳过"); return; }
            _ros2.PublishBool("/execute", true);
            Debug.Log("[Ros2PlanningBridge] /execute 已发布 (Bool)");
        }

        // ═══════════════════════════════════════════════
        // 订阅（Init 时一次性注册）
        // ═══════════════════════════════════════════════

        private void SubscribeAll()
        {
            // FK 结果 ← /ee_pose
            _ros2.SubscribePoseStamped("/ee_pose", msg =>
            {
                if (OnFkResult == null) return;
                try
                {
                    var pose = JointTrajectoryMapper.ToDevicePose(msg);
                    OnFkResult.Invoke(pose);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Ros2PlanningBridge] FK 结果解析失败: {ex.Message}");
                }
            });

            // IK 结果 ← /planned_joints（7 DOF 关节角，弧度 → 度）
            _ros2.Subscribe<JointStateMsg>("/planned_joints", msg =>
            {
                if (OnIkResult == null) return;
                try
                {
                    var angles = JointTrajectoryMapper.ToJointAnglesDeg(msg);
                    OnIkResult.Invoke(angles);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Ros2PlanningBridge] IK 结果解析失败: {ex.Message}");
                }
            });

            // 规划状态 ← /plan_status
            _ros2.SubscribeString("/plan_status", msg =>
            {
                OnPlanStatus?.Invoke(msg.data);
            });
        }
    }
}
