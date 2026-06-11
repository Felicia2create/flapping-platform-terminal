using System;
using System.Collections.Generic;

namespace FPT.Core
{
    /// <summary>
    /// 变频器/电机控制器状态 — 📋 预留
    /// </summary>
    public class VfdMotorState : IDeviceState
    {
        public string DeviceId { get; set; }
        public DeviceConnectionState Connection { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public Dictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();

        // === 变频器/电机特有字段 ===

        /// <summary> 当前输出频率 (Hz) </summary>
        public double CurrentFrequency { get; set; }

        /// <summary> 目标频率 (Hz) </summary>
        public double TargetFrequency { get; set; }

        /// <summary> 电机转速 (RPM) </summary>
        public double MotorSpeed { get; set; }

        /// <summary> 输出电流 (A) </summary>
        public double OutputCurrent { get; set; }

        /// <summary> 输出电压 (V) </summary>
        public double OutputVoltage { get; set; }

        /// <summary> 运行状态 </summary>
        public VfdRunStatus RunStatus { get; set; }

        // TODO: 后续根据实际变频器型号和 Modbus 寄存器映射扩展
    }

    /// <summary>
    /// 变频器运行状态
    /// </summary>
    public enum VfdRunStatus
    {
        Stopped = 0,
        Running = 1,
        Accelerating = 2,
        Decelerating = 3,
        Fault = 4,
    }
}
