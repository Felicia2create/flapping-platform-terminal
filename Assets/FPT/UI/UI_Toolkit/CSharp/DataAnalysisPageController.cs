using System;
using System.Collections.Generic;
using FPT.Business;
using FPT.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace FPT.UI
{
    /// <summary>
    /// 数据分析页控制器 — 订阅 ArmDriver 实时状态，按固定频率采样各传感器通道，
    /// 用户可任选纵坐标通道，横坐标统一为时间，折线图展示。
    /// 通道：关节角度/速度/力矩 J1~J6、转台角度、末端位置 XYZ、末端姿态 RPY。
    /// </summary>
    public class DataAnalysisPageController : IDisposable
    {
        private readonly RobotArmDriver _armDriver;

        // ── 通道定义 ──
        private class Channel
        {
            public string Label;                 // 下拉菜单显示名
            public string Unit;                  // 单位后缀
            public int ValueDecimals;            // 数值小数位
            public Func<RobotArmState, double?> Read;  // 从状态取值（null = 无数据）
            public TimeSeriesChart Chart;
        }

        private readonly List<Channel> _channels = new List<Channel>();
        private int _activeChannel;

        // ── UI 元素 ──
        private readonly DropdownField _channelSelector;
        private readonly DropdownField _windowSelector;
        private readonly Label _chartTitle;
        private readonly Label _chartValue;
        private readonly Label _yMax, _yMid, _yMin;
        private readonly Label _xT0, _xT1;
        private readonly Label _statCurrent, _statMax, _statMin, _statCount;
        private readonly Button _pauseBtn, _clearBtn;

        // ── 采样 ──
        private const double SampleInterval = 0.05;   // 20 Hz
        private const int ChartCapacity = 4096;        // 120s × 20Hz = 2400，留余量
        private double _lastSampleTime = -1;
        private bool _paused;

        private static readonly float[] WindowOptions = { 10f, 30f, 60f, 120f };
        private static readonly string[] WindowLabels = { "10 秒", "30 秒", "60 秒", "120 秒" };
        private float _window = 60f;

        public DataAnalysisPageController(VisualElement root, RobotArmDriver armDriver)
        {
            _armDriver = armDriver;

            // ── 查询 UI ──
            _chartTitle = root.Q<Label>("DaChartTitle");
            _chartValue = root.Q<Label>("DaChartValue");
            _yMax = root.Q<Label>("DaYMax");
            _yMid = root.Q<Label>("DaYMid");
            _yMin = root.Q<Label>("DaYMin");
            _xT0 = root.Q<Label>("DaXT0");
            _xT1 = root.Q<Label>("DaXT1");
            _statCurrent = root.Q<Label>("DaStatCurrent");
            _statMax = root.Q<Label>("DaStatMax");
            _statMin = root.Q<Label>("DaStatMin");
            _statCount = root.Q<Label>("DaStatCount");
            _pauseBtn = root.Q<Button>("DaPauseButton");
            _clearBtn = root.Q<Button>("DaClearButton");
            var plot = root.Q("DaChartPlot");

            // ── 建立通道（颜色按数据类型分组）──
            var cAngle = new Color(0.12f, 0.47f, 0.90f);   // 蓝
            var cVel = new Color(0.00f, 0.66f, 0.75f);     // 青
            var cTorque = new Color(0.90f, 0.45f, 0.10f);  // 橙
            var cPos = new Color(0.55f, 0.35f, 0.80f);     // 紫
            var cRot = new Color(0.20f, 0.65f, 0.35f);     // 绿

            for (int j = 0; j < 6; j++)
            {
                int joint = j;
                AddChannel($"角度 J{j + 1}", "°", 1, plot, cAngle,
                    st => ReadJoint(st.JointNames, st.JointAngles, joint));
            }
            for (int j = 0; j < 6; j++)
            {
                int joint = j;
                AddChannel($"速度 J{j + 1}", " rad/s", 3, plot, cVel,
                    st => ReadJoint(st.JointNames, st.JointVelocities, joint));
            }
            for (int j = 0; j < 6; j++)
            {
                int joint = j;
                AddChannel($"力矩 J{j + 1}", " Nm", 2, plot, cTorque,
                    st => ReadJoint(st.JointNames, st.JointTorques, joint));
            }
            AddChannel("转台角度", "°", 1, plot, cAngle, st =>
            {
                var idx = Array.FindIndex(st.JointNames, n => n != null && n.Contains("plate"));
                return idx >= 0 && idx < st.JointAngles.Length ? st.JointAngles[idx] : (double?)null;
            });
            AddChannel("末端位置 X", " m", 3, plot, cPos, st => st.EndEffectorPose.X);
            AddChannel("末端位置 Y", " m", 3, plot, cPos, st => st.EndEffectorPose.Y);
            AddChannel("末端位置 Z", " m", 3, plot, cPos, st => st.EndEffectorPose.Z);
            AddChannel("末端姿态 R", "°", 1, plot, cRot, st => st.EndEffectorPose.Roll);
            AddChannel("末端姿态 P", "°", 1, plot, cRot, st => st.EndEffectorPose.Pitch);
            AddChannel("末端姿态 Y", "°", 1, plot, cRot, st => st.EndEffectorPose.Yaw);

            // ── 通道下拉菜单 ──
            var channelContainer = root.Q("DaChannelContainer");
            if (channelContainer != null)
            {
                var names = new List<string>();
                foreach (var c in _channels) names.Add(c.Label);
                _channelSelector = new DropdownField(names, 0);
                _channelSelector.AddToClassList("speed-slider");
                _channelSelector.AddToClassList("fx-dropdown");
                _channelSelector.style.flexGrow = 1;
                channelContainer.Add(_channelSelector);
                _channelSelector.RegisterValueChangedCallback(_ => SelectChannel(_channelSelector.index));
            }

            // ── 时间窗口下拉菜单 ──
            var windowContainer = root.Q("DaWindowContainer");
            if (windowContainer != null)
            {
                _windowSelector = new DropdownField(new List<string>(WindowLabels), 2); // 默认 60s
                _windowSelector.AddToClassList("speed-slider");
                _windowSelector.AddToClassList("fx-dropdown");
                _windowSelector.style.flexGrow = 1;
                windowContainer.Add(_windowSelector);
                _windowSelector.RegisterValueChangedCallback(_ => SetWindow(WindowOptions[_windowSelector.index]));
            }
            SetWindow(_window);

            // ── 按钮 ──
            if (_pauseBtn != null) _pauseBtn.clicked += OnPauseClicked;
            if (_clearBtn != null) _clearBtn.clicked += OnClearClicked;

            SelectChannel(0);

            // ── 订阅实时状态 ──
            if (_armDriver != null)
                _armDriver.OnStateChanged += OnArmStateChanged;
        }

        private void AddChannel(string label, string unit, int decimals, VisualElement plot,
            Color color, Func<RobotArmState, double?> read)
        {
            _channels.Add(new Channel
            {
                Label = label,
                Unit = unit,
                ValueDecimals = decimals,
                Read = read,
                Chart = new TimeSeriesChart(plot, ChartCapacity, color)
            });
        }

        /// <summary> 按关节名（J1~J6）从数组取值；数组缺失或过短返回 null </summary>
        private static double? ReadJoint(string[] names, double[] values, int jointIndex)
        {
            if (names == null || values == null) return null;
            for (int i = 0; i < names.Length && i < values.Length; i++)
            {
                if (JointIndexFromName(names[i]) == jointIndex)
                    return values[i];
            }
            return null;
        }

        private static int JointIndexFromName(string name)
        {
            if (string.IsNullOrEmpty(name)) return -1;
            for (int i = name.Length - 1; i >= 0; i--)
            {
                if (name[i] >= '0' && name[i] <= '9')
                {
                    var n = name[i] - '0';
                    if (n >= 1 && n <= 6) return n - 1;
                    break;
                }
            }
            return -1;
        }

        private void OnArmStateChanged(IDeviceState state)
        {
            if (_paused || state is not RobotArmState arm) return;

            double t = Time.realtimeSinceStartupAsDouble;
            if (t - _lastSampleTime < SampleInterval) return;
            _lastSampleTime = t;

            foreach (var c in _channels)
            {
                var v = c.Read(arm);
                if (v.HasValue)
                    c.Chart.Push(t, (float)v.Value);
            }
            RefreshLabels();
        }

        private void SelectChannel(int index)
        {
            if (index < 0 || index >= _channels.Count) return;
            _activeChannel = index;
            for (int i = 0; i < _channels.Count; i++)
                _channels[i].Chart.Active = i == index;

            var c = _channels[index];
            if (_chartTitle != null) _chartTitle.text = c.Label;
            RefreshLabels();
            c.Chart.Repaint();
        }

        private void SetWindow(float seconds)
        {
            _window = seconds;
            foreach (var c in _channels) c.Chart.WindowSeconds = seconds;
            if (_xT0 != null) _xT0.text = $"-{seconds:F0}s";
            if (_xT1 != null) _xT1.text = $"-{seconds / 2f:F0}s";
            RefreshLabels();
        }

        private void OnPauseClicked()
        {
            _paused = !_paused;
            if (_pauseBtn != null) _pauseBtn.text = _paused ? "▶ 继续" : "⏸ 暂停";
        }

        private void OnClearClicked()
        {
            foreach (var c in _channels) c.Chart.Clear();
            RefreshLabels();
        }

        /// <summary> 刷新当前值 / 值域 / 统计标签 </summary>
        private void RefreshLabels()
        {
            var c = _channels[_activeChannel];
            string fmt = $"F{c.ValueDecimals}";

            if (c.Chart.TryGetLatest(out float latest))
            {
                if (_chartValue != null) _chartValue.text = $"{latest.ToString(fmt)}{c.Unit}";
                if (_statCurrent != null) _statCurrent.text = $"{latest.ToString(fmt)}{c.Unit}";
            }
            else
            {
                if (_chartValue != null) _chartValue.text = "暂无数据";
                if (_statCurrent != null) _statCurrent.text = "--";
            }

            if (c.Chart.TryGetVisibleRange(out float min, out float max))
            {
                float mid = (min + max) * 0.5f;
                if (_yMax != null) _yMax.text = max.ToString(fmt);
                if (_yMid != null) _yMid.text = mid.ToString(fmt);
                if (_yMin != null) _yMin.text = min.ToString(fmt);
                if (_statMax != null) _statMax.text = $"{max.ToString(fmt)}{c.Unit}";
                if (_statMin != null) _statMin.text = $"{min.ToString(fmt)}{c.Unit}";
            }
            else
            {
                if (_yMax != null) _yMax.text = "--";
                if (_yMid != null) _yMid.text = "--";
                if (_yMin != null) _yMin.text = "--";
                if (_statMax != null) _statMax.text = "--";
                if (_statMin != null) _statMin.text = "--";
            }

            if (_statCount != null) _statCount.text = c.Chart.Count.ToString();
        }

        public void Dispose()
        {
            if (_armDriver != null)
                _armDriver.OnStateChanged -= OnArmStateChanged;
            if (_pauseBtn != null) _pauseBtn.clicked -= OnPauseClicked;
            if (_clearBtn != null) _clearBtn.clicked -= OnClearClicked;
        }
    }
}
