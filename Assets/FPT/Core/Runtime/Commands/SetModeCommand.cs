namespace FPT.Core
{
    /// <summary>
    /// 切换运行模式指令
    /// </summary>
    public class SetModeCommand : IDeviceCommand
    {
        public string TargetDeviceId { get; set; }
        public string CommandType => "SetModeCommand";
        public bool RequiresAcknowledgment { get; set; } = true;

        /// <summary> 目标运行模式（字符串，由各设备 Driver 解析）</summary>
        public string Mode { get; set; }

        public SetModeCommand() { }

        public SetModeCommand(string deviceId, string mode)
        {
            TargetDeviceId = deviceId;
            Mode = mode;
        }

        // 机械臂模式快捷方法
        public static SetModeCommand ArmJointSpace(string deviceId)
            => new SetModeCommand(deviceId, "JointSpace");

        public static SetModeCommand ArmCartesianSpace(string deviceId)
            => new SetModeCommand(deviceId, "CartesianSpace");

        public static SetModeCommand ArmTeaching(string deviceId)
            => new SetModeCommand(deviceId, "Teaching");

        // 转台模式快捷方法
        public static SetModeCommand TurntablePosition(string deviceId)
            => new SetModeCommand(deviceId, "PositionMode");

        public static SetModeCommand TurntableVelocity(string deviceId)
            => new SetModeCommand(deviceId, "VelocityMode");
    }
}
