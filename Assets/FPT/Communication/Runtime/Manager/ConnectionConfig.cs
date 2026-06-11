namespace FPT.Communication
{
    /// <summary>
    /// 连接配置 — 非 ROS2 设备（串口等）
    /// ROS2 设备走 Ros2Bridge，不使用此配置。
    /// </summary>
    public class ConnectionConfig
    {
        public string DeviceId { get; set; }
        public TransportType Type { get; set; } = TransportType.Serial;

        // 串口
        public string PortName { get; set; } = "COM3";
        public int BaudRate { get; set; } = 115200;

        // 通用
        public int ConnectTimeoutMs { get; set; } = 5000;
        public int ReceiveTimeoutMs { get; set; } = 3000;
        public int SendTimeoutMs { get; set; } = 3000;
        public int MaxRetries { get; set; } = 3;
        public int RetryBaseDelayMs { get; set; } = 1000;
        public int RetryMaxDelayMs { get; set; } = 30000;

        public override string ToString() => $"Serial {PortName}@{BaudRate}";
    }

    public enum TransportType
    {
        Serial = 0,
    }
}
