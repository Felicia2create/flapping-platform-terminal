using System.Threading.Tasks;
using FPT.Core;

namespace FPT.Business
{
    /// <summary>
    /// 指令拦截器接口 — 责任链模式中的一环
    /// 新增校验逻辑 = 新增一个 ICommandInterceptor 实现
    /// </summary>
    public interface ICommandInterceptor
    {
        /// <summary>
        /// 拦截指令
        /// </summary>
        /// <returns>null = 通过；CommandResult = 拦截（校验失败）</returns>
        Task<CommandResult> Intercept(IDeviceCommand command, IDeviceState currentState);

        /// <summary> 拦截器名称（用于日志）</summary>
        string Name { get; }
    }
}
