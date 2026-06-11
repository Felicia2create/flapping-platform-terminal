using System;
using System.Collections.Generic;

namespace FPT.Core
{
    /// <summary>
    /// 机械臂状态 — 先实现
    /// </summary>
    public class RobotArmState : IDeviceState
    {
        public string DeviceId { get; set; }
        public DeviceConnectionState Connection { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public Dictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();

        // === 机械臂特有字段 ===

        /// <summary> ROS 关节名（与 JointAngles 同序），例: ["joint1","joint2",...] 或 ["arm1_joint1",...]</summary>
        public string[] JointNames { get; set; } = System.Array.Empty<string>();

        /// <summary> 各关节角度（度），长度 = 关节数 </summary>
        public double[] JointAngles { get; set; } = System.Array.Empty<double>();

        /// <summary> 各关节速度（度/秒）</summary>
        public double[] JointVelocities { get; set; } = System.Array.Empty<double>();

        /// <summary> 各关节力矩（Nm）</summary>
        public double[] JointTorques { get; set; } = System.Array.Empty<double>();

        /// <summary> 末端执行器位姿 </summary>
        public DevicePose EndEffectorPose { get; set; } = DevicePose.Identity;

        /// <summary> 夹爪开度（0.0 = 全闭, 1.0 = 全开）</summary>
        public double GripperOpening { get; set; }

        /// <summary> 当前运行模式 </summary>
        public ArmOperationMode Mode { get; set; } = ArmOperationMode.Idle;

        /// <summary> 关节数量 </summary>
        public int JointCount => JointAngles?.Length ?? 0;

        public override string ToString()
            => $"[RobotArm:{DeviceId}] Mode={Mode}, Joints=[{string.Join(", ", JointAngles ?? System.Array.Empty<double>())}], EE={EndEffectorPose}";
    }
}
