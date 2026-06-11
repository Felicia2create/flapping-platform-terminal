using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FPT.Core;

namespace FPT.Business
{
    /// <summary>
    /// 变频器/电机驱动 — 📋 预留
    /// 后续实现 Modbus RTU 或自定义帧的编码/解码
    /// </summary>
    public class VfdMotorDriver : DeviceDriverBase
    {
        private VfdMotorState _state;

        public override string DeviceId { get; }
        public override DeviceInfo Info { get; }
        public override IDeviceState CurrentState => _state;

        public override IReadOnlyList<Type> SupportedCommandTypes { get; }
            = new List<Type>
            {
                typeof(StopCommand),
                // TODO: 后续添加变频器专用指令（SetFrequencyCommand 等）
            }.AsReadOnly();

        public VfdMotorDriver(string deviceId)
        {
            DeviceId = deviceId;
            Info = new DeviceInfo
            {
                DeviceId = deviceId,
                DisplayName = "变频器",
                Type = DeviceType.VfdController,
            };
            _state = new VfdMotorState
            {
                DeviceId = deviceId,
                Connection = DeviceConnectionState.Disconnected,
            };
        }

        protected override void ConfigurePipeline(CommandPipeline pipeline)
        {
            // TODO: 后续添加频率限制、电流限制等拦截器
        }

        protected override Task<CommandResult> SendCommandToDevice(IDeviceCommand command)
        {
            // TODO: 后续实现变频器指令编码（Modbus RTU 或自定义帧）
            return Task.FromResult(CommandResult.Fail("变频器驱动尚未实现"));
        }

        // TODO: 后续实现变频器状态更新回调
        // public void OnVfdStatusReceived(VfdMotorState state) { ... }
    }
}
