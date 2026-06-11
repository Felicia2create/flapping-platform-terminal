using System;
using FPT.Core;

namespace FPT.Communication
{
    /// <summary>
    /// 消息路由接口 — 按消息类型将解码后的消息分发到订阅者
    /// 观察者模式：新增消息类型 = 定义新类 + Subscribe<T>()
    /// </summary>
    public interface IMessageRouter
    {
        /// <summary>
        /// 订阅指定类型的消息
        /// </summary>
        /// <returns>取消订阅的 Disposable</returns>
        IDisposable Subscribe<T>(Action<T> handler) where T : IMessage;

        /// <summary>
        /// 分发消息到所有匹配的订阅者
        /// </summary>
        void Route(IMessage message);

        /// <summary>
        /// 清除所有订阅
        /// </summary>
        void Clear();
    }
}
