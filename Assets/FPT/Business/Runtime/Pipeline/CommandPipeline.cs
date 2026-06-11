using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FPT.Core;

namespace FPT.Business
{
    /// <summary>
    /// 指令管道 — 责任链模式
    ///
    /// 流程：Interceptor1 → Interceptor2 → ... → 发送到设备
    /// 任意一环返回非 null = 拦截，提前终止
    /// </summary>
    public class CommandPipeline
    {
        private readonly List<ICommandInterceptor> _interceptors
            = new List<ICommandInterceptor>();

        /// <summary>
        /// 添加拦截器 — Fluent API
        /// </summary>
        public CommandPipeline AddInterceptor(ICommandInterceptor interceptor)
        {
            _interceptors.Add(interceptor);
            return this;
        }

        /// <summary>
        /// 移除拦截器
        /// </summary>
        public CommandPipeline RemoveInterceptor(ICommandInterceptor interceptor)
        {
            _interceptors.Remove(interceptor);
            return this;
        }

        /// <summary>
        /// 执行管道：逐级拦截，全部通过后调用 sendToDevice
        /// </summary>
        public async Task<CommandResult> Execute(
            IDeviceCommand command,
            IDeviceState currentState,
            Func<IDeviceCommand, Task<CommandResult>> sendToDevice)
        {
            foreach (var interceptor in _interceptors)
            {
                var result = await interceptor.Intercept(command, currentState);

                if (result != null)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[CommandPipeline] 指令被拦截: {interceptor.Name} → {result.Message}");
                    return result;
                }
            }

            // 全部通过 → 发送
            return await sendToDevice(command);
        }

        /// <summary>
        /// 获取已注册的拦截器名称列表
        /// </summary>
        public IReadOnlyList<string> InterceptorNames
        {
            get
            {
                var names = new List<string>();
                foreach (var interceptor in _interceptors)
                    names.Add(interceptor.Name);
                return names;
            }
        }
    }
}
