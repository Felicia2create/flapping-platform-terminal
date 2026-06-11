namespace FPT.Core
{
    /// <summary>
    /// 笛卡尔空间运动指令 — 控制末端执行器位姿
    /// </summary>
    public class EePoseCommand : IDeviceCommand
    {
        public string TargetDeviceId { get; set; }
        public string CommandType => "EePoseCommand";
        public bool RequiresAcknowledgment { get; set; } = true;

        /// <summary> 目标末端位姿 </summary>
        public DevicePose TargetPose { get; set; } = DevicePose.Identity;

        /// <summary> 运动速度（m/s），0 = 默认 </summary>
        public double LinearVelocity { get; set; }

        /// <summary> 角速度（度/秒），0 = 默认 </summary>
        public double AngularVelocity { get; set; }

        public EePoseCommand() { }

        public EePoseCommand(string deviceId, DevicePose targetPose)
        {
            TargetDeviceId = deviceId;
            TargetPose = targetPose;
        }
    }
}
