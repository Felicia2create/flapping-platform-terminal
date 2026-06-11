namespace FPT.Core
{
    /// <summary>
    /// 紧急停止 / 暂停指令 — 所有设备通用
    /// </summary>
    public class StopCommand : IDeviceCommand
    {
        public string TargetDeviceId { get; set; }
        public string CommandType => "StopCommand";
        public bool RequiresAcknowledgment { get; set; } = true;

        /// <summary> true = 紧急停止（立即断电）, false = 正常停止 </summary>
        public bool IsEmergency { get; set; }

        public StopCommand() { }

        public StopCommand(string deviceId, bool isEmergency = false)
        {
            TargetDeviceId = deviceId;
            IsEmergency = isEmergency;
        }
    }
}
