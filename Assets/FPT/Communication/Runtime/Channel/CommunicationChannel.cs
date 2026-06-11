using System;
using System.Threading.Tasks;
using FPT.Core;

namespace FPT.Communication
{
    public class CommunicationChannel : ICommunicationChannel
    {
        private readonly ITransport _transport;
        private readonly IProtocolCodec _codec;
        private readonly IMessageRouter _router;

        public string DeviceId { get; }
        public TransportState State { get; private set; } = TransportState.Disconnected;
        public bool IsConnected => State == TransportState.Connected;
        public event Action<TransportState> OnStateChanged;

        public CommunicationChannel(string deviceId, ITransport transport, IProtocolCodec codec, IMessageRouter router)
        {
            DeviceId = deviceId;
            _transport = transport;
            _codec = codec;
            _router = router;
            _transport.OnDataReceived += OnData;
            _transport.OnStateChanged += s => { State = s; OnStateChanged?.Invoke(s); };
        }

        public Task ConnectAsync() { return Task.CompletedTask; }
        public Task DisconnectAsync() => _transport.DisconnectAsync();

        public Task SendCommand(IDeviceCommand command)
            => SendAsync(new CommandMessage { SourceDeviceId = DeviceId, CommandData = command });

        public Task SendAsync(IMessage message)
            => _transport.SendAsync(_codec.Encode(message));

        public IDisposable Subscribe<T>(Action<T> handler) where T : IMessage
            => _router.Subscribe(handler);

        private void OnData(byte[] data)
        {
            var r = _codec.Decode(data);
            if (r?.IsComplete == true && r.Message != null) _router.Route(r.Message);
        }

        public void Dispose()
        {
            _transport.OnDataReceived -= OnData;
            _transport.Dispose();
        }
    }

    internal class CommandMessage : IMessage, IDeviceMessage
    {
        public ushort MessageTypeId => 0x00FF;
        public string MessageTypeName => "CommandMessage";
        public string SourceDeviceId { get; set; }
        public IDeviceCommand CommandData { get; set; }
    }
}
