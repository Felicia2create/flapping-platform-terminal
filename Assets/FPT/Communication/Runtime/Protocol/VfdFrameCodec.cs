using FPT.Core;

namespace FPT.Communication
{
    /// <summary>
    /// 变频器帧编解码 — 📋 预留
    ///
    /// 支持两种常用格式（根据实际变频器型号选择）：
    ///
    /// Modbus RTU:
    ///   SlaveAddr(1B) | FuncCode(1B) | Data(NB) | CRC16(2B, little-endian)
    ///
    /// 自定义帧:
    ///   Header(1B) | Addr(1B) | Cmd(1B) | DataLen(1B) | Data(NB) | Checksum(1B)
    /// </summary>
    public class VfdFrameCodec : IProtocolCodec
    {
        private byte[] _receiveBuffer = System.Array.Empty<byte>();

        // TODO: 后续根据实际变频器型号定义帧格式
        // private const byte FrameHeader = 0xAA;
        // private const byte SlaveAddress = 0x01;

        public byte[] Encode(IMessage message)
        {
            // TODO: 实现变频器指令帧编码
            // Modbus RTU: CRC16 校验
            // 自定义帧: 简单 Checksum
            throw new System.NotImplementedException("VfdFrameCodec 尚未实现，预留接口供后续扩展");
        }

        public DecodeResult Decode(byte[] rawData)
        {
            // TODO: 实现变频器响应帧解码
            // 1. 累积缓冲区
            // 2. 根据协议类型解析（Modbus 或自定义）
            // 3. CRC16 / Checksum 校验
            // 4. 映射到 VfdMotorState 字段
            throw new System.NotImplementedException("VfdFrameCodec 尚未实现，预留接口供后续扩展");
        }
    }
}
