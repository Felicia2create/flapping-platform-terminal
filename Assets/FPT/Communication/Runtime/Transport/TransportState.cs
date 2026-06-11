namespace FPT.Communication
{
    /// <summary>
    /// 传输层连接状态
    /// </summary>
    public enum TransportState
    {
        Disconnected = 0,
        Connecting = 1,
        Connected = 2,
        Error = 3,
        Disconnecting = 4,
    }
}
