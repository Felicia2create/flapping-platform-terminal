using System;
using FPT.Business;
using FPT.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace FPT.UI
{
    /// <summary>
    /// 顶部栏控制器 — 订阅 DeviceManager 事件，显示连接状态和 FPS
    /// </summary>
    public class TopBarController : IDisposable
    {
        private readonly VisualElement _root;
        private readonly DeviceManager _deviceManager;

        private readonly Label _connectionLabel;
        private readonly VisualElement _connectionIndicator;
        private readonly VisualElement _connectionPill;
        private readonly Label _modeLabel;
        private readonly Label _fpsLabel;

        private int _frameCount;
        private float _timeAccum;

        public TopBarController(VisualElement root, DeviceManager deviceManager)
        {
            _root = root;
            _deviceManager = deviceManager;

            _connectionLabel = root?.Q<Label>("ConnectionLabel");
            _connectionIndicator = root?.Q<VisualElement>("ConnectionIndicator");
            _connectionPill = root?.Q<VisualElement>("ConnectionPill");
            _modeLabel = root?.Q<Label>("ModeLabel");
            _fpsLabel = root?.Q<Label>("FpsLabel");

            // 订阅设备状态变更
            _deviceManager.OnAnyDeviceStateChanged += OnDeviceStateChanged;

            // FPS 更新通过主循环
            var runner = GameObject.FindObjectOfType<MonoBehaviour>();
            if (runner != null)
                runner.StartCoroutine(FpsUpdateLoop());
        }

        private void OnDeviceStateChanged(IDeviceState state)
        {
            if (state is RobotArmState arm)
            {
                _connectionLabel.text = arm.Connection switch
                {
                    DeviceConnectionState.Operational => "机械臂 · 运行中",
                    DeviceConnectionState.Connected => "机械臂 · 已连接",
                    DeviceConnectionState.Connecting => "机械臂 · 连接中...",
                    DeviceConnectionState.Error => "机械臂 · 异常",
                    _ => "机械臂 · 未连接",
                };

                var dotClass = arm.Connection switch
                {
                    DeviceConnectionState.Operational => "connected",
                    DeviceConnectionState.Error => "error",
                    _ => "disconnected",
                };

                ApplyStateClass(_connectionIndicator, dotClass);
                ApplyStateClass(_connectionPill, dotClass);

                // 模式状态由控制面板 ModeHintLabel / PlanStatusLabel 展示
            }
        }

        private static void ApplyStateClass(VisualElement element, string stateClass)
        {
            if (element == null) return;
            element.RemoveFromClassList("connected");
            element.RemoveFromClassList("disconnected");
            element.RemoveFromClassList("error");
            element.AddToClassList(stateClass);
        }

        private System.Collections.IEnumerator FpsUpdateLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.5f);
                var fps = (int)(1f / Time.unscaledDeltaTime);
                _fpsLabel.text = $"FPS {fps}";
            }
        }

        public void Dispose()
        {
            _deviceManager.OnAnyDeviceStateChanged -= OnDeviceStateChanged;
        }
    }
}
