namespace FPT.Core
{
    /// <summary>
    /// 设备指令基接口 — 所有设备指令必须实现
    /// </summary>
    public interface IDeviceCommand
    {
        /// <summary> 目标设备 ID </summary>
        string TargetDeviceId { get; }

        /// <summary> 指令类型标识（用于日志/序列化）</summary>
        string CommandType { get; }

        /// <summary> 是否需要等待设备确认 </summary>
        bool RequiresAcknowledgment { get; }
    }
}
