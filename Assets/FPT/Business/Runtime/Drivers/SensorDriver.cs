using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FPT.Core;

namespace FPT.Business
{
    /// <summary>
    /// 传感器模块驱动 — 📋 预留
    /// 后续实现串口数据解析 + 传感器状态更新
    /// </summary>
    public class SensorDriver : DeviceDriverBase
    {
        private SensorState _state;

        public override string DeviceId { get; }
        public override DeviceInfo Info { get; }
        public override IDeviceState CurrentState => _state;

        public override IReadOnlyList<Type> SupportedCommandTypes { get; }
            = new List<Type>().AsReadOnly(); // 传感器通常只上报，不接收指令

        public SensorDriver(string deviceId)
        {
            DeviceId = deviceId;
            Info = new DeviceInfo
            {
                DeviceId = deviceId,
                DisplayName = "传感器模块",
                Type = DeviceType.SensorModule,
            };
            _state = new SensorState
            {
                DeviceId = deviceId,
                Connection = DeviceConnectionState.Disconnected,
            };
        }

        protected override void ConfigurePipeline(CommandPipeline pipeline)
        {
            // 传感器通常无需拦截
        }

        protected override Task<CommandResult> SendCommandToDevice(IDeviceCommand command)
        {
            // TODO: 后续实现传感器配置指令（如设置采样率）
            return Task.FromResult(CommandResult.Fail("传感器驱动尚未实现"));
        }

        // TODO: 后续实现传感器数据更新回调
        // public void OnSensorDataReceived(Dictionary<string, double> readings) { ... }
    }
}
