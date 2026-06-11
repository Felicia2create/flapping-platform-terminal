using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FPT.UI
{
    /// <summary>
    /// 底部状态栏控制器 — 显示消息日志
    /// </summary>
    public class StatusBarController : IDisposable
    {
        private readonly Label _statusMessage;
        private readonly Label _statusTime;

        public StatusBarController(VisualElement root)
        {
            _statusMessage = root.Q<Label>("StatusMessage");
            _statusTime = root.Q<Label>("StatusTime");
        }

        /// <summary>
        /// 在状态栏显示消息（3 秒后自动清除）
        /// </summary>
        public void ShowMessage(string message)
        {
            _statusMessage.text = message;
            _statusTime.text = DateTime.Now.ToString("HH:mm:ss");
        }

        public void Dispose()
        {
            // nothing to clean
        }
    }
}
