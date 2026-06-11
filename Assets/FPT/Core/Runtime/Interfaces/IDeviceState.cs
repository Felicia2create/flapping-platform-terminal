using System;

namespace FPT.Core
{
    /// <summary>
    /// 设备状态基接口 — 所有设备状态必须实现
    /// </summary>
    public interface IDeviceState
    {
        /// <summary> 设备唯一标识 </summary>
        string DeviceId { get; }

        /// <summary> 连接状态 </summary>
        DeviceConnectionState Connection { get; }

        /// <summary> 最后更新时间 </summary>
        DateTime LastUpdateTime { get; }

        /// <summary> 通用指标字典（可扩展）</summary>
        System.Collections.Generic.Dictionary<string, double> Metrics { get; }
    }
}
