namespace FPT.Core
{
    /// <summary>
    /// 回零指令 — 转台/机械臂通用
    /// </summary>
    public class HomeCommand : IDeviceCommand
    {
        public string TargetDeviceId { get; set; }
        public string CommandType => "HomeCommand";
        public bool RequiresAcknowledgment { get; set; } = true;

        public HomeCommand() { }

        public HomeCommand(string deviceId)
        {
            TargetDeviceId = deviceId;
        }
    }
}
