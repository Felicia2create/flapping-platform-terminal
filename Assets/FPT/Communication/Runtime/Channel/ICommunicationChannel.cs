using System;
using System.Threading.Tasks;
using FPT.Core;

namespace FPT.Communication
{
    public interface ICommunicationChannel : IDeviceChannel, IDisposable
    {
        string DeviceId { get; }
        TransportState State { get; }
        Task ConnectAsync();
        Task DisconnectAsync();
        Task SendCommand(IDeviceCommand command);
        event Action<TransportState> OnStateChanged;
    }
}
