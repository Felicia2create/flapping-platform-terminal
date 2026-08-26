using System;
using UnityEngine.UIElements;

namespace FPT.UI
{
    /// <summary>
    /// 底部状态栏控制器 — 全局消息（3 秒自动清除）+ 实时时钟 + ROS 端点
    /// Tick() 由 MainViewController.Update 驱动
    /// </summary>
    public class StatusBarController : IDisposable
    {
        private readonly Label _statusMessage;
        private readonly Label _statusTime;
        private readonly Label _statusEndpoint;

        private const float MessageHoldS = 3f;
        private float _messageTimer = -1f;
        private string _defaultMessage = "就绪";
        private float _clockAccum;

        public StatusBarController(VisualElement root)
        {
            _statusMessage = root?.Q<Label>("StatusMessage");
            _statusTime = root?.Q<Label>("StatusTime");
            _statusEndpoint = root?.Q<Label>("StatusEndpoint");
            UpdateClock();
        }

        /// <summary> 每帧由 MainViewController 调用 </summary>
        public void Tick(float deltaTime)
        {
            // 时钟每秒刷新一次
            _clockAccum += deltaTime;
            if (_clockAccum >= 1f)
            {
                _clockAccum = 0f;
                UpdateClock();
            }

            // 临时消息到时自动清除
            if (_messageTimer > 0f)
            {
                _messageTimer -= deltaTime;
                if (_messageTimer <= 0f && _statusMessage != null)
                    _statusMessage.text = _defaultMessage;
            }
        }

        /// <summary> 在状态栏显示消息（3 秒后自动恢复默认文案）</summary>
        public void ShowMessage(string message)
        {
            if (_statusMessage != null) _statusMessage.text = message;
            _messageTimer = MessageHoldS;
            UpdateClock();
        }

        /// <summary> 更新 ROS 端点显示（连接状态变化时调用）</summary>
        public void SetEndpoint(string text)
        {
            if (_statusEndpoint != null) _statusEndpoint.text = text;
        }

        private void UpdateClock()
        {
            if (_statusTime != null)
                _statusTime.text = DateTime.Now.ToString("HH:mm:ss");
        }

        public void Dispose()
        {
            // nothing to clean
        }
    }
}
