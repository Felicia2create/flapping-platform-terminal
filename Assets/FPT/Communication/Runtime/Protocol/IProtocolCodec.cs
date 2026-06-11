using FPT.Core;

namespace FPT.Communication
{
    /// <summary>
    /// 协议编解码接口 — 将强类型消息与原始字节互转
    /// 适配器模式：换协议格式 = 换一个 IProtocolCodec 实现
    /// </summary>
    public interface IProtocolCodec
    {
        /// <summary>
        /// 编码：业务消息 → 原始字节（用于发送）
        /// </summary>
        byte[] Encode(IMessage message);

        /// <summary>
        /// 解码：原始字节 → 业务消息（用于接收）
        /// </summary>
        DecodeResult Decode(byte[] rawData);
    }

    /// <summary>
    /// 解码结果
    /// </summary>
    public class DecodeResult
    {
        /// <summary> 解析出的消息（null = 数据不完整，需继续累积）</summary>
        public IMessage Message { get; set; }

        /// <summary> 消耗的字节数（用于处理粘包）</summary>
        public int ConsumedBytes { get; set; }

        /// <summary> 是否完整帧 </summary>
        public bool IsComplete { get; set; }

        public static DecodeResult Incomplete()
            => new DecodeResult { IsComplete = false, ConsumedBytes = 0 };

        public static DecodeResult Success(IMessage message, int consumedBytes)
            => new DecodeResult
            {
                IsComplete = true,
                Message = message,
                ConsumedBytes = consumedBytes,
            };
    }
}
