using System;
using System.Collections.Generic;
using FPT.Core;

namespace FPT.Communication
{
    /// <summary>
    /// 消息路由器 — 线程安全的类型化消息分发
    /// </summary>
    public class MessageRouter : IMessageRouter
    {
        // 类型 → 处理器列表
        private readonly Dictionary<Type, List<object>> _handlers
            = new Dictionary<Type, List<object>>();

        private readonly object _lock = new object();

        public IDisposable Subscribe<T>(Action<T> handler) where T : IMessage
        {
            var type = typeof(T);
            lock (_lock)
            {
                if (!_handlers.TryGetValue(type, out var list))
                {
                    list = new List<object>();
                    _handlers[type] = list;
                }
                list.Add(handler);
            }

            return new Unsubscriber(() =>
            {
                lock (_lock)
                {
                    if (_handlers.TryGetValue(type, out var list))
                        list.Remove(handler);
                }
            });
        }

        public void Route(IMessage message)
        {
            if (message == null) return;

            var type = message.GetType();
            List<object> handlers;

            lock (_lock)
            {
                if (!_handlers.TryGetValue(type, out handlers))
                    return;

                // 复制一份避免在回调中修改集合
                handlers = new List<object>(handlers);
            }

            foreach (var handler in handlers)
            {
                try
                {
                    ((Action<object>)handler)(message);
                }
                catch (Exception ex)
                {
                    // 一个处理器出错不影响其他处理器
                    UnityEngine.Debug.LogError(
                        $"[MessageRouter] 处理消息 {type.Name} 时出错: {ex.Message}");
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _handlers.Clear();
            }
        }

        private class Unsubscriber : IDisposable
        {
            private readonly Action _unsubscribe;
            private bool _disposed;

            public Unsubscriber(Action unsubscribe)
            {
                _unsubscribe = unsubscribe;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _unsubscribe?.Invoke();
                    _disposed = true;
                }
            }
        }
    }
}
