using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FPT.Core;

namespace FPT.Business
{
    /// <summary>
    /// 设备驱动抽象基类 — 提供通用状态机、健康监控、指令管道
    /// 具体设备只需继承此类，实现自己的状态更新和指令发送逻辑
    /// </summary>
    public abstract class DeviceDriverBase : IDeviceDriver
    {
        protected IDeviceChannel Channel { get; private set; }
        protected DeviceStateMachine StateMachine { get; }
        protected CommandPipeline Pipeline { get; }

        public abstract string DeviceId { get; }
        public abstract DeviceInfo Info { get; }
        public bool IsInitialized { get; protected set; }

        public abstract IDeviceState CurrentState { get; }
        public event Action<IDeviceState> OnStateChanged;

        public abstract IReadOnlyList<Type> SupportedCommandTypes { get; }

        public DeviceHealth Health { get; protected set; } = new DeviceHealth();
        public event Action<DeviceHealth> OnHealthChanged;

        protected DeviceDriverBase()
        {
            StateMachine = new DeviceStateMachine();
            Pipeline = new CommandPipeline();
            StateMachine.OnStateChanged += (_, newState) =>
            {
                UpdateHealthFromState(newState);
            };
        }

        public virtual async Task InitializeAsync(IDeviceChannel channel)
        {
            Channel = channel ?? throw new ArgumentNullException(nameof(channel));
            ConfigurePipeline(Pipeline);

            StateMachine.TryTransitionTo(DeviceConnectionState.Connecting);
            StateMachine.TryTransitionTo(DeviceConnectionState.Connected);
            StateMachine.TryTransitionTo(DeviceConnectionState.Initializing);

            await OnInitializeAsync();

            StateMachine.TryTransitionTo(DeviceConnectionState.Operational);
            IsInitialized = true;
        }

        public virtual async Task ShutdownAsync()
        {
            StateMachine.TryTransitionTo(DeviceConnectionState.Disconnecting);
            await OnShutdownAsync();
            StateMachine.TryTransitionTo(DeviceConnectionState.Disconnected);
            IsInitialized = false;
        }

        public virtual async Task<CommandResult> ExecuteCommand(IDeviceCommand command)
        {
            if (!IsInitialized)
                return CommandResult.Fail("设备未初始化");

            if (StateMachine.CurrentState != DeviceConnectionState.Operational)
                return CommandResult.Fail($"设备状态异常: {StateMachine.CurrentState}");

            // 检查指令是否支持
            if (!IsCommandSupported(command.GetType()))
                return CommandResult.Fail($"不支持的指令类型: {command.GetType().Name}");

            // 走管道
            return await Pipeline.Execute(command, CurrentState, SendCommandToDevice);
        }

        /// <summary>
        /// 子类实现：具体发送逻辑
        /// </summary>
        protected abstract Task<CommandResult> SendCommandToDevice(IDeviceCommand command);

        /// <summary>
        /// 子类实现：配置指令管道（添加 Interceptor）
        /// </summary>
        protected abstract void ConfigurePipeline(CommandPipeline pipeline);

        /// <summary>
        /// 子类可选重写：初始化时的自定义逻辑
        /// </summary>
        protected virtual Task OnInitializeAsync() => Task.CompletedTask;

        /// <summary>
        /// 子类可选重写：关闭时的自定义逻辑
        /// </summary>
        protected virtual Task OnShutdownAsync() => Task.CompletedTask;

        /// <summary>
        /// 触发状态变更事件
        /// </summary>
        protected void NotifyStateChanged(IDeviceState state)
        {
            OnStateChanged?.Invoke(state);
        }

        /// <summary>
        /// 触发健康状态变更事件
        /// </summary>
        protected void NotifyHealthChanged(DeviceHealth health)
        {
            Health = health;
            OnHealthChanged?.Invoke(health);
        }

        /// <summary>
        /// 检查指令类型是否在支持列表中
        /// </summary>
        protected bool IsCommandSupported(Type commandType)
        {
            foreach (var t in SupportedCommandTypes)
            {
                if (t.IsAssignableFrom(commandType))
                    return true;
            }
            return false;
        }

        private void UpdateHealthFromState(DeviceConnectionState state)
        {
            Health = state switch
            {
                DeviceConnectionState.Operational => new DeviceHealth
                    { Level = HealthLevel.Healthy },
                DeviceConnectionState.Error => new DeviceHealth
                    { Level = HealthLevel.Error, Messages = new[] { "设备异常" } },
                DeviceConnectionState.Disconnected => new DeviceHealth
                    { Level = HealthLevel.Disconnected },
                _ => Health,
            };
            OnHealthChanged?.Invoke(Health);
        }
    }
}
