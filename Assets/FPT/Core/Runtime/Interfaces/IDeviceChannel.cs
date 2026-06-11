using System;
using System.Threading.Tasks;

namespace FPT.Core
{
    /// <summary>
    /// 设备通信通道接口 — Core 层的最小抽象
    /// Communication 层的 ICommunicationChannel 扩展此接口
    /// </summary>
    public interface IDeviceChannel
    {
        /// <summary> 通道是否已连接 </summary>
        bool IsConnected { get; }

        /// <summary> 发送消息 </summary>
        Task SendAsync(IMessage message);

        /// <summary> 订阅指定类型的消息 </summary>
        IDisposable Subscribe<T>(Action<T> handler) where T : IMessage;
    }
}
