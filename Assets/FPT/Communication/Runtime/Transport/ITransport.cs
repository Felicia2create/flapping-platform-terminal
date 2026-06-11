using System;
using System.Threading.Tasks;

namespace FPT.Communication
{
    /// <summary>
    /// 传输层抽象接口 — 纯字节收发，不关心上层协议
    /// 策略模式：换传输方式 = 换一个 ITransport 实现
    /// </summary>
    public interface ITransport : IDisposable
    {
        /// <summary> 当前传输状态 </summary>
        TransportState State { get; }

        /// <summary> 收到数据时触发 </summary>
        event Action<byte[]> OnDataReceived;

        /// <summary> 传输状态变更时触发 </summary>
        event Action<TransportState> OnStateChanged;

        /// <summary> 建立连接 </summary>
        Task ConnectAsync(ConnectionConfig config);

        /// <summary> 发送字节数据 </summary>
        Task SendAsync(byte[] data);

        /// <summary> 断开连接 </summary>
        Task DisconnectAsync();
    }
}
