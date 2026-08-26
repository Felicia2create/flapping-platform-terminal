using System;
using FPT.Business;
using FPT.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace FPT.UI
{
    /// <summary>
    /// 顶部栏控制器 — 连接状态胶囊、FPS（滑动平均）、模式徽标三态（IDLE/PLANNING/ESTOP）
    /// Tick() 由 MainViewController.Update 驱动，不再借用随机 MonoBehaviour 开协程
    /// </summary>
    public class TopBarController : IDisposable
    {
        /// <summary> 模式徽标状态 </summary>
        public enum Badge { Idle, Planning, Estop }

        private readonly DeviceManager _deviceManager;

        private readonly Label _connectionLabel;
        private readonly VisualElement _connectionIndicator;
        private readonly VisualElement _connectionPill;
        private readonly VisualElement _modeBadge;
        private readonly Label _modeLabel;
        private readonly Label _fpsLabel;

        // FPS 滑动平均
        private int _frameCount;
        private float _fpsAccum;
        private const float FpsWindow = 0.5f;

        // ESTOP 徽标自动复位
        private float _estopTimer = -1f;
        private const float EstopHoldS = 5f;
        private Badge _badge = Badge.Idle;

        public TopBarController(VisualElement root, DeviceManager deviceManager)
        {
            _deviceManager = deviceManager;

            _connectionLabel = root?.Q<Label>("ConnectionLabel");
            _connectionIndicator = root?.Q<VisualElement>("ConnectionIndicator");
            _connectionPill = root?.Q<VisualElement>("ConnectionPill");
            _modeBadge = root?.Q<VisualElement>("ModeBadge");
            _modeLabel = root?.Q<Label>("ModeLabel");
            _fpsLabel = root?.Q<Label>("FpsLabel");

            _deviceManager.OnAnyDeviceStateChanged += OnDeviceStateChanged;
            ApplyBadge();
        }

        /// <summary> 每帧由 MainViewController 调用 </summary>
        public void Tick()
        {
            // FPS：0.5s 窗口内的平均帧率
            _frameCount++;
            _fpsAccum += Time.unscaledDeltaTime;
            if (_fpsAccum >= FpsWindow)
            {
                if (_fpsLabel != null)
                    _fpsLabel.text = $"FPS {Mathf.RoundToInt(_frameCount / _fpsAccum)}";
                _frameCount = 0;
                _fpsAccum = 0f;
            }

            // ESTOP 徽标持有一段时间后自动复位
            if (_estopTimer > 0f)
            {
                _estopTimer -= Time.unscaledDeltaTime;
                if (_estopTimer <= 0f)
                {
                    _badge = Badge.Idle;
                    ApplyBadge();
                }
            }
        }

        /// <summary> 切换徽标状态（Estop 会保持 5 秒后自动回到 Idle）</summary>
        public void SetBadge(Badge badge)
        {
            _badge = badge;
            _estopTimer = badge == Badge.Estop ? EstopHoldS : -1f;
            ApplyBadge();
        }

        private void ApplyBadge()
        {
            if (_modeBadge == null || _modeLabel == null) return;

            _modeBadge.RemoveFromClassList("badge-planning");
            _modeBadge.RemoveFromClassList("badge-estop");
            switch (_badge)
            {
                case Badge.Planning:
                    _modeBadge.AddToClassList("badge-planning");
                    _modeLabel.text = "PLANNING";
                    break;
                case Badge.Estop:
                    _modeBadge.AddToClassList("badge-estop");
                    _modeLabel.text = "ESTOP";
                    break;
                default:
                    _modeLabel.text = "IDLE";
                    break;
            }
        }

        private void OnDeviceStateChanged(IDeviceState state)
        {
            if (state is not RobotArmState arm) return;

            if (_connectionLabel != null)
            {
                _connectionLabel.text = arm.Connection switch
                {
                    DeviceConnectionState.Operational => "机械臂 · 运行中",
                    DeviceConnectionState.Connected => "机械臂 · 已连接",
                    DeviceConnectionState.Connecting => "机械臂 · 连接中...",
                    DeviceConnectionState.Error => "机械臂 · 异常",
                    _ => "机械臂 · 未连接",
                };
            }

            var dotClass = arm.Connection switch
            {
                DeviceConnectionState.Operational => "connected",
                DeviceConnectionState.Error => "error",
                _ => "disconnected",
            };

            ApplyStateClass(_connectionIndicator, dotClass);
            ApplyStateClass(_connectionPill, dotClass);
        }

        private static void ApplyStateClass(VisualElement element, string stateClass)
        {
            if (element == null) return;
            element.RemoveFromClassList("connected");
            element.RemoveFromClassList("disconnected");
            element.RemoveFromClassList("error");
            element.AddToClassList(stateClass);
        }

        public void Dispose()
        {
            _deviceManager.OnAnyDeviceStateChanged -= OnDeviceStateChanged;
        }
    }
}
