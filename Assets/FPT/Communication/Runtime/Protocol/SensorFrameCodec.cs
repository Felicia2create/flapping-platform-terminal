using FPT.Core;

namespace FPT.Communication
{
    /// <summary>
    /// 传感器自定义帧编解码 — 📋 预留
    ///
    /// 典型帧格式（根据实际传感器协议调整）：
    ///   Header(1B: 0xAA) | SensorID(1B) | DataLen(1B) | Data(N×4B) | Checksum(1B)
    /// </summary>
    public class SensorFrameCodec : IProtocolCodec
    {
        private byte[] _receiveBuffer = System.Array.Empty<byte>();

        // TODO: 后续根据实际传感器协议定义帧格式常量
        // private const byte FrameHeader = 0xAA;

        public byte[] Encode(IMessage message)
        {
            // TODO: 实现传感器指令帧编码
            // 例如查询指令: Header + CmdID + Checksum
            throw new System.NotImplementedException("SensorFrameCodec 尚未实现，预留接口供后续扩展");
        }

        public DecodeResult Decode(byte[] rawData)
        {
            // TODO: 实现传感器数据帧解码
            // 1. 累积缓冲区
            // 2. 查找帧头 0xAA
            // 3. 读取 SensorID + DataLen
            // 4. 校验 Checksum
            // 5. 解析数据字段
            throw new System.NotImplementedException("SensorFrameCodec 尚未实现，预留接口供后续扩展");
        }
    }
}
