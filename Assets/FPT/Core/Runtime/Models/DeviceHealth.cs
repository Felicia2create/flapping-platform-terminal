namespace FPT.Core
{
    /// <summary>
    /// 设备健康 / 诊断状态
    /// </summary>
    public class DeviceHealth
    {
        /// <summary> 健康等级 </summary>
        public HealthLevel Level { get; set; } = HealthLevel.Unknown;

        /// <summary> 最后收到消息的时间戳 </summary>
        public double LastHeartbeatTime { get; set; }

        /// <summary> 心跳超时（秒）</summary>
        public double HeartbeatTimeout { get; set; }

        /// <summary> 心跳是否超时 </summary>
        public bool IsHeartbeatTimeout { get; set; }

        /// <summary> 错误/警告信息列表 </summary>
        public string[] Messages { get; set; } = System.Array.Empty<string>();
    }

    public enum HealthLevel
    {
        Unknown = 0,
        Healthy = 1,
        Warning = 2,
        Error = 3,
        Disconnected = 4,
    }
}
