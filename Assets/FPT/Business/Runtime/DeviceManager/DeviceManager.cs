using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FPT.Core;

namespace FPT.Business
{
    /// <summary>
    /// 设备管理器 — 统一管理所有 IDeviceDriver 的注册、初始化、生命周期
    /// 核心设计：不关心具体设备类型，只管理 Driver 集合
    /// </summary>
    public class DeviceManager
    {
        private readonly Dictionary<string, IDeviceDriver> _drivers
            = new Dictionary<string, IDeviceDriver>();

        private readonly List<IDeviceDriver> _orderedDrivers
            = new List<IDeviceDriver>();

        /// <summary>
        /// 所有已注册的设备驱动
        /// </summary>
        public IReadOnlyList<IDeviceDriver> Drivers => _orderedDrivers.AsReadOnly();

        /// <summary>
        /// 任意设备状态变更事件 — UI 层可订阅此统一入口
        /// </summary>
        public event Action<IDeviceState> OnAnyDeviceStateChanged;

        public DeviceManager()
        {
        }

        /// <summary>
        /// 注册设备驱动 — 核心扩展点：新增设备 = 新建 Driver 并注册
        /// </summary>
        public void RegisterDriver(IDeviceDriver driver)
        {
            if (_drivers.ContainsKey(driver.DeviceId))
                throw new InvalidOperationException(
                    $"设备 {driver.DeviceId} 已注册");

            _drivers[driver.DeviceId] = driver;
            _orderedDrivers.Add(driver);
        }

        /// <summary>
        /// 按顺序初始化所有已注册的设备驱动
        /// </summary>
        public async Task InitializeAllAsync()
        {
            foreach (var driver in _orderedDrivers)
            {
                await InitializeDriverAsync(driver);
            }
        }

        /// <summary>
        /// 初始化单个设备驱动
        /// </summary>
        public async Task InitializeDriverAsync(IDeviceDriver driver)
        {
            // 初始化驱动（ROS2 设备通过 Ros2Node 通信，不需要 channel）
            await driver.InitializeAsync(null);

            // 绑定状态变更事件
            driver.OnStateChanged += state =>
            {
                OnAnyDeviceStateChanged?.Invoke(state);
            };

            UnityEngine.Debug.Log(
                $"[DeviceManager] 设备 {driver.DeviceId} 已初始化");
        }

        /// <summary>
        /// 关闭所有设备
        /// </summary>
        public async Task ShutdownAllAsync()
        {
            foreach (var driver in _orderedDrivers)
            {
                await driver.ShutdownAsync();
            }
        }

        /// <summary>
        /// 按 ID 获取设备驱动
        /// </summary>
        public IDeviceDriver GetDriver(string deviceId)
        {
            return _drivers.TryGetValue(deviceId, out var driver)
                ? driver
                : null;
        }

        /// <summary>
        /// 获取指定设备状态
        /// </summary>
        public IDeviceState GetDeviceState(string deviceId)
        {
            return _drivers.TryGetValue(deviceId, out var driver)
                ? driver.CurrentState
                : null;
        }

        /// <summary>
        /// 获取所有设备状态
        /// </summary>
        public Dictionary<string, IDeviceState> AllStates
            => _drivers.ToDictionary(kv => kv.Key, kv => kv.Value.CurrentState);
    }
}
