using System;
using FPT.Core;

namespace FPT.Business
{
    /// <summary>
    /// 设备连接状态机 — 所有设备 Driver 共用
    ///
    /// 状态转换：
    ///   Disconnected → Connecting → Connected → Initializing → Operational
    ///   任意状态 → Error → Disconnecting → Disconnected
    /// </summary>
    public class DeviceStateMachine
    {
        public DeviceConnectionState CurrentState { get; private set; }
            = DeviceConnectionState.Disconnected;

        /// <summary> 状态变更事件 </summary>
        public event Action<DeviceConnectionState, DeviceConnectionState> OnStateChanged;

        /// <summary>
        /// 尝试转换到目标状态（仅允许合法转换）
        /// </summary>
        /// <returns>是否成功转换</returns>
        public bool TryTransitionTo(DeviceConnectionState target)
        {
            if (!IsValidTransition(CurrentState, target))
                return false;

            var oldState = CurrentState;
            CurrentState = target;
            OnStateChanged?.Invoke(oldState, target);
            return true;
        }

        /// <summary>
        /// 强制转换（跳过合法性检查，用于异常处理）
        /// </summary>
        public void ForceTransitionTo(DeviceConnectionState target)
        {
            var oldState = CurrentState;
            CurrentState = target;
            OnStateChanged?.Invoke(oldState, target);
        }

        /// <summary>
        /// 判断状态转换是否合法
        /// </summary>
        private bool IsValidTransition(DeviceConnectionState from, DeviceConnectionState to)
        {
            if (from == to) return true;

            return (from, to) switch
            {
                // 启动流程
                (DeviceConnectionState.Disconnected, DeviceConnectionState.Connecting) => true,
                (DeviceConnectionState.Connecting, DeviceConnectionState.Connected) => true,
                (DeviceConnectionState.Connected, DeviceConnectionState.Initializing) => true,
                (DeviceConnectionState.Initializing, DeviceConnectionState.Operational) => true,

                // 正常关闭
                (DeviceConnectionState.Operational, DeviceConnectionState.Disconnecting) => true,
                (DeviceConnectionState.Disconnecting, DeviceConnectionState.Disconnected) => true,

                // 连接失败回退
                (DeviceConnectionState.Connecting, DeviceConnectionState.Error) => true,
                (DeviceConnectionState.Initializing, DeviceConnectionState.Error) => true,

                // 错误恢复
                (DeviceConnectionState.Error, DeviceConnectionState.Disconnecting) => true,
                (DeviceConnectionState.Error, DeviceConnectionState.Disconnected) => true,

                // 运行中错误
                (DeviceConnectionState.Operational, DeviceConnectionState.Error) => true,

                // 任意状态可断开
                (_, DeviceConnectionState.Disconnecting) => true,

                _ => false,
            };
        }
    }
}
