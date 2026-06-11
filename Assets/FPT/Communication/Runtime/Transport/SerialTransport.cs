using System;
using System.Threading.Tasks;

namespace FPT.Communication
{
    /// <summary>
    /// 串口传输 — 📋 预留接口（后续补全 System.IO.Ports.SerialPort 逻辑）
    /// RS-485 和 UART 在软件层面共用此类，仅电气层不同
    /// </summary>
    public class SerialTransport : ITransport
    {
        public TransportState State { get; private set; } = TransportState.Disconnected;

#pragma warning disable CS0067 // 预留桩代码，事件暂未使用
        public event Action<byte[]> OnDataReceived;
        public event Action<TransportState> OnStateChanged;
#pragma warning restore CS0067

        // TODO: 后续引入 System.IO.Ports.SerialPort
        // private SerialPort _serialPort;

        public string PortName { get; set; } = "COM3";
        public int BaudRate { get; set; } = 115200;
        public int DataBits { get; set; } = 8;
        // TODO: 后续引入 System.IO.Ports 后替换为 Parity/StopBits 枚举
        public int Parity { get; set; } = 0;      // 0=None, 1=Odd, 2=Even
        public int StopBits { get; set; } = 1;     // 1, 2

        /// <summary>
        /// TODO: 后续实现串口连接
        /// 1. 创建 SerialPort 实例，配置 PortName / BaudRate / DataBits / Parity / StopBits
        /// 2. 调用 _serialPort.Open()
        /// 3. 启动后台线程/异步循环读取 DataReceived 事件
        /// 4. 收到数据 → OnDataReceived
        /// 5. 错误时 → OnStateChanged(Error)
        /// </summary>
        public Task ConnectAsync(ConnectionConfig config)
        {
            // TODO: 后续实现
            // PortName = config.PortName;
            // BaudRate = config.BaudRate;
            // _serialPort = new SerialPort(PortName, BaudRate, Parity, DataBits, StopBits);
            // _serialPort.ReadTimeout = config.ReceiveTimeoutMs;
            // _serialPort.WriteTimeout = config.SendTimeoutMs;
            // _serialPort.Open();

            throw new NotImplementedException("SerialTransport 尚未实现，预留接口供后续扩展");
        }

        /// <summary>
        /// TODO: 后续实现串口发送
        /// _serialPort.Write(data, 0, data.Length);
        /// </summary>
        public Task SendAsync(byte[] data)
        {
            throw new NotImplementedException("SerialTransport 尚未实现，预留接口供后续扩展");
        }

        /// <summary>
        /// TODO: 后续实现串口断开
        /// _serialPort?.Close();
        /// _serialPort?.Dispose();
        /// </summary>
        public Task DisconnectAsync()
        {
            throw new NotImplementedException("SerialTransport 尚未实现，预留接口供后续扩展");
        }

        public void Dispose()
        {
            // TODO: 后续释放串口资源
            // _serialPort?.Dispose();
        }
    }
}
