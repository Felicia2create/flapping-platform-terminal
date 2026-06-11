using System;
using System.Collections.Generic;

namespace FPT.Core
{
    /// <summary>
    /// 传感器模块状态 — 📋 预留
    /// </summary>
    public class SensorState : IDeviceState
    {
        public string DeviceId { get; set; }
        public DeviceConnectionState Connection { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public Dictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();

        // === 传感器特有字段 ===

        /// <summary> 传感器读数（键 = 传感器名, 值 = 数值）</summary>
        public Dictionary<string, double> Readings { get; set; } = new Dictionary<string, double>();

        /// <summary> 传感器数量 </summary>
        public int SensorCount => Readings.Count;

        // TODO: 后续根据实际传感器类型扩展具体字段
        // 例如: 温度、压力、力传感器、IMU 等
    }
}
