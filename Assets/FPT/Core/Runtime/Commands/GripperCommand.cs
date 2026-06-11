namespace FPT.Core
{
    /// <summary>
    /// 夹爪控制指令
    /// </summary>
    public class GripperCommand : IDeviceCommand
    {
        public string TargetDeviceId { get; set; }
        public string CommandType => "GripperCommand";
        public bool RequiresAcknowledgment { get; set; } = true;

        /// <summary> 目标开度（0.0 = 全闭, 1.0 = 全开）</summary>
        public double Opening { get; set; }

        /// <summary> 夹持力（N），0 = 默认 </summary>
        public double Force { get; set; }

        /// <summary> 夹持速度（0-1），0 = 默认 </summary>
        public double Speed { get; set; }

        public GripperCommand() { }

        public GripperCommand(string deviceId, double opening)
        {
            TargetDeviceId = deviceId;
            Opening = opening;
        }

        /// <summary> 快捷：全闭 </summary>
        public static GripperCommand Close(string deviceId)
            => new GripperCommand(deviceId, 0.0);

        /// <summary> 快捷：全开 </summary>
        public static GripperCommand Open(string deviceId)
            => new GripperCommand(deviceId, 1.0);
    }
}
