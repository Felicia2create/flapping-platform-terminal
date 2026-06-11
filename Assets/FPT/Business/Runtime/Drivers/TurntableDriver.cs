using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FPT.Core;

namespace FPT.Business
{
    /// <summary>
    /// 转台设备驱动 — 📋 预留
    /// 后续实现 485 协议通信 + 转台状态更新
    /// </summary>
    public class TurntableDriver : DeviceDriverBase
    {
        private TurntableState _state;

        public override string DeviceId { get; }
        public override DeviceInfo Info { get; }
        public override IDeviceState CurrentState => _state;

        public override IReadOnlyList<Type> SupportedCommandTypes { get; }
            = new List<Type>
            {
                typeof(StopCommand),
                typeof(HomeCommand),
                typeof(SetModeCommand),
                // TODO: 后续添加转台专用指令（RotateCommand 等）
            }.AsReadOnly();

        public TurntableDriver(string deviceId)
        {
            DeviceId = deviceId;
            Info = new DeviceInfo
            {
                DeviceId = deviceId,
                DisplayName = "转台",
                Type = DeviceType.Turntable,
            };
            _state = new TurntableState
            {
                DeviceId = deviceId,
                Connection = DeviceConnectionState.Disconnected,
            };
        }

        protected override void ConfigurePipeline(CommandPipeline pipeline)
        {
            // TODO: 后续添加角度限位、速度限制等拦截器
        }

        protected override Task<CommandResult> SendCommandToDevice(IDeviceCommand command)
        {
            // TODO: 后续实现转台指令编码（485 自定义帧）
            return Task.FromResult(CommandResult.Fail("转台驱动尚未实现"));
        }

        // TODO: 后续实现转台状态更新回调
        // public void OnTurntableStateReceived(TurntableState state) { ... }
    }
}
