using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FPT.Communication
{
    /// <summary>
    /// 通信管理器 — 管理非 ROS2 设备的通信通道（串口等）
    /// ROS2 设备走 Ros2Bridge，不走此管理器。
    /// </summary>
    public class CommunicationManager
    {
        private readonly Dictionary<string, ICommunicationChannel> _channels = new();

        public IReadOnlyDictionary<string, ICommunicationChannel> ActiveChannels => _channels;

        public async Task<ICommunicationChannel> OpenChannel(ConnectionConfig config)
        {
            if (_channels.ContainsKey(config.DeviceId))
                throw new InvalidOperationException($"设备 {config.DeviceId} 已存在通道");

            ITransport transport = config.Type switch
            {
                TransportType.Serial => new SerialTransport(),
                _ => throw new ArgumentException($"不支持的传输类型: {config.Type}"),
            };

            var codec = CreateCodecForDevice(config.DeviceId);
            var router = new MessageRouter();
            var channel = new CommunicationChannel(config.DeviceId, transport, codec, router);

            await transport.ConnectAsync(config);
            _channels[config.DeviceId] = channel;
            return channel;
        }

        public async Task CloseChannel(string deviceId)
        {
            if (_channels.TryGetValue(deviceId, out var channel))
            {
                await channel.DisconnectAsync();
                channel.Dispose();
                _channels.Remove(deviceId);
            }
        }

        public async Task CloseAllChannels()
        {
            foreach (var id in new List<string>(_channels.Keys))
                await CloseChannel(id);
        }

        private IProtocolCodec CreateCodecForDevice(string deviceId) => deviceId switch
        {
            "sensors"   => new SensorFrameCodec(),
            "vfd_motor" => new VfdFrameCodec(),
            _           => null,
        };
    }
}
