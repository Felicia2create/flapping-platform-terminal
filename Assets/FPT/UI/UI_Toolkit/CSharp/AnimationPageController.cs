using System.Collections.Generic;
using FPT.Business;
using UnityEngine;
using UnityEngine.UIElements;

namespace FPT.UI
{
    public class AnimationPageController : System.IDisposable
    {
        private readonly AnimationDemoController _demo;

        // 播放控制
        private readonly Button _playBtn;
        private readonly Label _statusLabel;

        // 参数滑块
        private readonly Slider _speedSlider;
        private readonly Slider _amplitudeSlider;
        private readonly Slider _frequencySlider;
        private readonly Slider _plateSpeedSlider;
        private readonly Label _speedValueLabel;
        private readonly Label _amplitudeValueLabel;
        private readonly Label _frequencyValueLabel;
        private readonly Label _plateSpeedValueLabel;

        // 模式下拉菜单（中文）
        private readonly DropdownField _modeSelector;
        // private static readonly List<string> _modeOptions = new List<string> { "波浪接力", "呼吸聚散", "8字轨迹" };
                private static readonly List<string> _modeOptions = new List<string> { "波浪接力", "呼吸聚散", "8字轨迹", "人字形", "Y字形" };

        // 机械臂关节实时回显
        private readonly Label[] _jointLabels = new Label[6];

        // 飞行参数数值 + 折线图
        private readonly Label _freqVal, _ampVal, _speedVal, _aoaVal, _liftVal, _altVal;
        private readonly MiniLineChart _freqChart, _ampChart, _speedChart, _aoaChart, _liftChart, _altChart;

        private const int ChartCapacity = 150;

        private bool _isPlaying;

        // 标记：是否正在从代码设置 UI 值（避免回调循环）
        private bool _suppressCallbacks;

        // ── 中文 ↔ 枚举映射 ──
        private static int GetModeIndex(DemoFormationMode mode) => mode switch
        {
            DemoFormationMode.SequentialWave => 0,
            DemoFormationMode.Breathing      => 1,
            DemoFormationMode.Lissajous      => 2,
            DemoFormationMode.VShape         => 3,
            DemoFormationMode.YShape         => 4,
            _ => 0
        };      

        private static DemoFormationMode GetModeFromIndex(int index) => index switch
        {
            0 => DemoFormationMode.SequentialWave,
            1 => DemoFormationMode.Breathing,
            2 => DemoFormationMode.Lissajous,
            3 => DemoFormationMode.VShape,
            4 => DemoFormationMode.YShape,
            _ => DemoFormationMode.SequentialWave
        };
        

        public AnimationPageController(VisualElement root, AnimationDemoController demo)
        {
            _demo = demo;

            _playBtn = root.Q<Button>("PlayButton");
            _speedSlider = root.Q<Slider>("SpeedSlider");
            _amplitudeSlider = root.Q<Slider>("AmplitudeSlider");
            _frequencySlider = root.Q<Slider>("FrequencySlider");
            _plateSpeedSlider = root.Q<Slider>("PlateSpeedSlider");
            _statusLabel = root.Q<Label>("AnimStatusLabel");
            _speedValueLabel = root.Q<Label>("SpeedValueLabel");
            _amplitudeValueLabel = root.Q<Label>("AmplitudeValueLabel");
            _frequencyValueLabel = root.Q<Label>("FrequencyValueLabel");
            _plateSpeedValueLabel = root.Q<Label>("PlateSpeedValueLabel");

            // ── 模式下拉菜单（DropdownField + 中文选项）──
            var modeContainer = root.Q<VisualElement>("ModeSelectorContainer");
            if (modeContainer != null)
            {
                _modeSelector = new DropdownField(_modeOptions, GetModeIndex(demo.CurrentMode));
                _modeSelector.AddToClassList("speed-slider");
                _modeSelector.style.flexGrow = 1;
                modeContainer.Add(_modeSelector);

                _modeSelector.RegisterValueChangedCallback(evt =>
                {
                    if (_suppressCallbacks) return;
                    _demo.CurrentMode = GetModeFromIndex(_modeSelector.index);
                });
            }

            // ── 播放按钮 ──
            if (_playBtn != null)
                _playBtn.clicked += OnPlayClicked;

            // ── 基础速度滑块 ──
            if (_speedSlider != null)
                _speedSlider.RegisterValueChangedCallback(evt =>
                {
                    if (_suppressCallbacks) return;
                    _demo.BaseSpeed = evt.newValue;
                    if (_speedValueLabel != null)
                        _speedValueLabel.text = $"{evt.newValue:F1}x";
                });

            // ── 动作幅度滑块 ──
            if (_amplitudeSlider != null)
                _amplitudeSlider.RegisterValueChangedCallback(evt =>
                {
                    if (_suppressCallbacks) return;
                    _demo.Amplitude = evt.newValue;
                    if (_amplitudeValueLabel != null)
                        _amplitudeValueLabel.text = $"{evt.newValue:F2} rad";
                });

            // ── 基础频率滑块 ──
            if (_frequencySlider != null)
                _frequencySlider.RegisterValueChangedCallback(evt =>
                {
                    if (_suppressCallbacks) return;
                    _demo.BaseFrequency = evt.newValue;
                    if (_frequencyValueLabel != null)
                        _frequencyValueLabel.text = $"{evt.newValue:F2} Hz";
                });

            // ── 转台转速滑块 ──
            if (_plateSpeedSlider != null)
                _plateSpeedSlider.RegisterValueChangedCallback(evt =>
                {
                    _demo.PlateSpeed = evt.newValue;
                    if (_plateSpeedValueLabel != null)
                        _plateSpeedValueLabel.text = $"{evt.newValue:F0}°/s";
                });

            // 机械臂关节回显
            for (int i = 0; i < 6; i++)
                _jointLabels[i] = root.Q<Label>($"AnimJLabel{i}");

            // 飞行参数数值
            _freqVal = root.Q<Label>("DroneFreqLabel");
            _ampVal = root.Q<Label>("DroneAmpLabel");
            _speedVal = root.Q<Label>("DroneSpeedLabel");
            _aoaVal = root.Q<Label>("DroneAoaLabel");
            _liftVal = root.Q<Label>("DroneLiftLabel");
            _altVal = root.Q<Label>("DroneAltLabel");

            // 飞行参数折线图
            var accent = new Color(0.12f, 0.47f, 0.90f);
            var accent2 = new Color(0f, 0.66f, 0.75f);
            _freqChart = new MiniLineChart(root.Q("ChartFreq"), ChartCapacity, accent);
            _ampChart = new MiniLineChart(root.Q("ChartAmp"), ChartCapacity, accent2);
            _speedChart = new MiniLineChart(root.Q("ChartSpeed"), ChartCapacity, accent);
            _aoaChart = new MiniLineChart(root.Q("ChartAoa"), ChartCapacity, accent2);
            _liftChart = new MiniLineChart(root.Q("ChartLift"), ChartCapacity, accent);
            _altChart = new MiniLineChart(root.Q("ChartAlt"), ChartCapacity, accent2);

            // 订阅底层事件
            _demo.OnArmPlayStateChanged += OnPlayStateChanged;
            _demo.OnArmAnglesChanged += OnArmAngles;
            _demo.OnFlightParamsChanged += OnFlightParams;

            // 从底层反向同步默认参数到 UI
            SyncUIFromDemo();
            UpdateUI();
        }

        /// <summary>
        /// 从 AnimationDemoController 读取当前参数值，反向同步到 UI 控件
        /// </summary>
        private void SyncUIFromDemo()
        {
            _suppressCallbacks = true;

            if (_modeSelector != null)
            {
                int idx = GetModeIndex(_demo.CurrentMode);
                _modeSelector.SetValueWithoutNotify(_modeOptions[idx]);
            }

            if (_speedSlider != null)
            {
                _speedSlider.SetValueWithoutNotify(_demo.BaseSpeed);
                if (_speedValueLabel != null)
                    _speedValueLabel.text = $"{_demo.BaseSpeed:F1}x";
            }

            if (_amplitudeSlider != null)
            {
                _amplitudeSlider.SetValueWithoutNotify(_demo.Amplitude);
                if (_amplitudeValueLabel != null)
                    _amplitudeValueLabel.text = $"{_demo.Amplitude:F2} rad";
            }

            if (_frequencySlider != null)
            {
                _frequencySlider.SetValueWithoutNotify(_demo.BaseFrequency);
                if (_frequencyValueLabel != null)
                    _frequencyValueLabel.text = $"{_demo.BaseFrequency:F2} Hz";
            }

            if (_plateSpeedSlider != null)
            {
                _plateSpeedSlider.SetValueWithoutNotify(_demo.PlateSpeed);
                if (_plateSpeedValueLabel != null)
                    _plateSpeedValueLabel.text = $"{_demo.PlateSpeed:F0}°/s";
            }

            _suppressCallbacks = false;
        }

        private void OnArmAngles(double[] anglesDeg)
        {
            for (int i = 0; i < 6 && i < anglesDeg.Length; i++)
                if (_jointLabels[i] != null)
                    _jointLabels[i].text = $"{anglesDeg[i]:F1}°";
        }

        private void OnFlightParams(FlapFlightParams f)
        {
            if (_freqVal != null) _freqVal.text = $"{f.FlapFrequencyHz:F2} Hz";
            if (_ampVal != null) _ampVal.text = $"{f.FlapAmplitudeDeg:F0}°";
            if (_speedVal != null) _speedVal.text = $"{f.AirspeedMps:F2} m/s";
            if (_aoaVal != null) _aoaVal.text = $"{f.AngleOfAttackDeg:F1}°";
            if (_liftVal != null) _liftVal.text = $"{f.LiftN:F1} N";
            if (_altVal != null) _altVal.text = $"{f.AltitudeM:F2} m";

            _freqChart.Push(f.FlapFrequencyHz);
            _ampChart.Push(f.FlapAmplitudeDeg);
            _speedChart.Push(f.AirspeedMps);
            _aoaChart.Push(f.AngleOfAttackDeg);
            _liftChart.Push(f.LiftN);
            _altChart.Push(f.AltitudeM);
        }

        private void ClearCharts()
        {
            _freqChart?.Clear();
            _ampChart?.Clear();
            _speedChart?.Clear();
            _aoaChart?.Clear();
            _liftChart?.Clear();
            _altChart?.Clear();
        }

        private void OnPlayClicked()
        {
            if (_isPlaying)
                _demo.PauseArm();
            else
                _demo.PlayArm();
        }

        private void OnPlayStateChanged(bool playing)
        {
            _isPlaying = playing;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = _isPlaying ? "▶ 播放中" : (_demo.IsArmPaused ? "⏸ 已暂停" : "■ 就绪");
                _statusLabel.RemoveFromClassList("playing");
                _statusLabel.RemoveFromClassList("paused");
                if (_isPlaying) _statusLabel.AddToClassList("playing");
                else if (_demo.IsArmPaused) _statusLabel.AddToClassList("paused");
            }

            if (_playBtn != null)
                _playBtn.text = _isPlaying ? "⏸ 暂停" : "▶ 播放";
        }

        public void Dispose()
        {
            if (_demo != null)
            {
                _demo.OnArmPlayStateChanged -= OnPlayStateChanged;
                _demo.OnArmAnglesChanged -= OnArmAngles;
                _demo.OnFlightParamsChanged -= OnFlightParams;
            }
            if (_playBtn != null) _playBtn.clicked -= OnPlayClicked;
        }
    }
}