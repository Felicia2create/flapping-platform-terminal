using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FPT.Core
{
    /// <summary>
    /// 设备驱动接口 — 每个物理设备对应一个 Driver 实现
    /// 这是整个系统的核心扩展点：新增设备 = 新增 IDeviceDriver 实现
    /// </summary>
    public interface IDeviceDriver
    {
        // === 身份 ===
        /// <summary> 设备唯一标识 </summary>
        string DeviceId { get; }

        /// <summary> 设备信息 </summary>
        DeviceInfo Info { get; }


        // === 生命周期 ===
        /// <summary> 初始化（传入通信通道）</summary>
        Task InitializeAsync(IDeviceChannel channel);

        /// <summary> 关闭驱动，释放资源 </summary>
        Task ShutdownAsync();

        /// <summary> 是否已初始化 </summary>
        bool IsInitialized { get; }


        // === 状态 ===
        /// <summary> 当前设备状态 </summary>
        IDeviceState CurrentState { get; }

        /// <summary> 状态变更事件 — UI 层订阅此事件刷新 </summary>
        event Action<IDeviceState> OnStateChanged;


        // === 指令 ===
        /// <summary> 向设备发送指令 </summary>
        Task<CommandResult> ExecuteCommand(IDeviceCommand command);

        /// <summary> 该设备支持的指令类型列表 </summary>
        IReadOnlyList<Type> SupportedCommandTypes { get; }


        // === 诊断 ===
        /// <summary> 当前健康状态 </summary>
        DeviceHealth Health { get; }

        /// <summary> 健康状态变更事件 </summary>
        event Action<DeviceHealth> OnHealthChanged;
    }
}
