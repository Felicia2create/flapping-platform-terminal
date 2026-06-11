namespace FPT.Core
{
    /// <summary>
    /// 通信消息基接口 — 所有网络消息必须实现
    /// </summary>
    public interface IMessage
    {
        /// <summary> 消息类型 ID（用于路由分发）</summary>
        ushort MessageTypeId { get; }

        /// <summary> 消息类型名称 </summary>
        string MessageTypeName { get; }
    }

    /// <summary>
    /// 设备数据消息 — 从设备上报的数据
    /// </summary>
    public interface IDeviceMessage : IMessage
    {
        /// <summary> 来源设备 ID </summary>
        string SourceDeviceId { get; }
    }
}
