using System;
using System.Collections.Generic;

namespace FPT.Core
{
    /// <summary>
    /// 转台状态 — 📋 预留
    /// </summary>
    public class TurntableState : IDeviceState
    {
        public string DeviceId { get; set; }
        public DeviceConnectionState Connection { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public Dictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();

        // === 转台特有字段 ===

        /// <summary> 当前角度（度）</summary>
        public double CurrentAngle { get; set; }

        /// <summary> 目标角度（度）</summary>
        public double TargetAngle { get; set; }

        /// <summary> 角速度（度/秒）</summary>
        public double AngularVelocity { get; set; }

        /// <summary> 运行模式 </summary>
        public TurntableMode Mode { get; set; }

        /// <summary> 是否已完成回零 </summary>
        public bool IsHomed { get; set; }

        /// <summary> 正限位触发 </summary>
        public bool PositiveLimit { get; set; }

        /// <summary> 负限位触发 </summary>
        public bool NegativeLimit { get; set; }

        // TODO: 后续根据实际转台型号和 485 协议扩展
    }

    public enum TurntableMode
    {
        Idle = 0,
        PositionMode = 1,
        VelocityMode = 2,
        Homing = 3,
        Error = 4,
    }
}
