namespace FPT.Core
{
    /// <summary>
    /// 关节空间运动指令 — 控制各关节角度
    /// </summary>
    public class JointCommand : IDeviceCommand
    {
        public string TargetDeviceId { get; set; }
        public string CommandType => "JointCommand";
        public bool RequiresAcknowledgment { get; set; } = true;

        /// <summary> 目标关节角度（度），长度 = 关节数 </summary>
        public double[] TargetAngles { get; set; }

        /// <summary> 目标关节速度（度/秒），null = 使用默认速度 </summary>
        public double[] TargetVelocities { get; set; }

        /// <summary> 运动时间（秒），0 = 尽快 </summary>
        public double Duration { get; set; }

        public JointCommand()
        {
            TargetAngles = System.Array.Empty<double>();
        }

        public JointCommand(string deviceId, double[] angles, double duration = 0)
        {
            TargetDeviceId = deviceId;
            TargetAngles = angles;
            Duration = duration;
        }
    }
}
