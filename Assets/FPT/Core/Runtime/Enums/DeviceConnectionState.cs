namespace FPT.Core
{
    /// <summary>
    /// 设备连接状态 — 所有设备共享的状态机
    /// Disconnected → Connecting → Connected → Initializing → Operational
    /// 任意状态 → Error → Disconnecting → Disconnected
    /// </summary>
    public enum DeviceConnectionState
    {
        /// <summary> 未连接 </summary>
        Disconnected = 0,

        /// <summary> 正在建立物理连接 </summary>
        Connecting = 1,

        /// <summary> 物理连接已建立，等待初始化 </summary>
        Connected = 2,

        /// <summary> 正在初始化（握手、获取初始状态）</summary>
        Initializing = 3,

        /// <summary> 正常运行 </summary>
        Operational = 4,

        /// <summary> 发生错误（可恢复）</summary>
        Error = 5,

        /// <summary> 正在断开连接 </summary>
        Disconnecting = 6,
    }
}
