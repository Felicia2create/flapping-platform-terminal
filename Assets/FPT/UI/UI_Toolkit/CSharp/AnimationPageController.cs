using FPT.Business;
using UnityEngine;
using UnityEngine.UIElements;

namespace FPT.UI
{
    public class AnimationPageController : System.IDisposable
    {
        private readonly AnimationDemoController _demo;

        private readonly Button _playBtn;
        private readonly Slider _speedSlider;
        private readonly Slider _plateSpeedSlider;
        private readonly Label _statusLabel;
        private readonly Label _speedValueLabel;
        private readonly Label _plateSpeedValueLabel;
        private readonly Button[] _pathButtons = new Button[4];

        // 机械臂关节实时回显
        private readonly Label[] _jointLabels = new Label[6];

        // 飞行参数数值 + 折线图
        private readonly Label _freqVal, _ampVal, _speedVal, _aoaVal, _liftVal, _altVal;
        private readonly MiniLineChart _freqChart, _ampChart, _speedChart, _aoaChart, _liftChart, _altChart;

        private const int ChartCapacity = 150;

        private bool _isPlaying;
        private int _selectedPath;

        public AnimationPageController(VisualElement root, AnimationDemoController demo)
        {
            _demo = demo;

            _playBtn = root.Q<Button>("PlayButton");
            _speedSlider = root.Q<Slider>("SpeedSlider");
            _plateSpeedSlider = root.Q<Slider>("PlateSpeedSlider");
            _statusLabel = root.Q<Label>("AnimStatusLabel");
            _speedValueLabel = root.Q<Label>("SpeedValueLabel");
            _plateSpeedValueLabel = root.Q<Label>("PlateSpeedValueLabel");

            if (_playBtn != null)
                _playBtn.clicked += OnPlayClicked;

            if (_speedSlider != null)
                _speedSlider.RegisterValueChangedCallback(evt =>
                {
                    _demo.PlaybackSpeed = evt.newValue;
                    if (_speedValueLabel != null)
                        _speedValueLabel.text = $"{evt.newValue:F1}x";
                });

            if (_plateSpeedSlider != null)
                _plateSpeedSlider.RegisterValueChangedCallback(evt =>
                {
                    _demo.PlateSpeed = evt.newValue;
                    if (_plateSpeedValueLabel != null)
                        _plateSpeedValueLabel.text = $"{evt.newValue:F0}°/s";
                });

            // 路径选择（4 条不同路径，切换选中态 + 后端切换轨迹）
            for (int i = 0; i < _pathButtons.Length; i++)
            {
                _pathButtons[i] = root.Q<Button>($"PathButton{i}");
                if (_pathButtons[i] != null)
                {
                    var idx = i;
                    _pathButtons[i].clicked += () => SelectPath(idx);
                }
            }

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

            // 飞行参数折线图（X 轴=时间）
            var accent = new Color(0.12f, 0.47f, 0.90f);
            var accent2 = new Color(0f, 0.66f, 0.75f);
            _freqChart = new MiniLineChart(root.Q("ChartFreq"), ChartCapacity, accent);
            _ampChart = new MiniLineChart(root.Q("ChartAmp"), ChartCapacity, accent2);
            _speedChart = new MiniLineChart(root.Q("ChartSpeed"), ChartCapacity, accent);
            _aoaChart = new MiniLineChart(root.Q("ChartAoa"), ChartCapacity, accent2);
            _liftChart = new MiniLineChart(root.Q("ChartLift"), ChartCapacity, accent);
            _altChart = new MiniLineChart(root.Q("ChartAlt"), ChartCapacity, accent2);

            _demo.OnArmPlayStateChanged += OnPlayStateChanged;
            _demo.OnArmAnglesChanged += OnArmAngles;
            _demo.OnFlightParamsChanged += OnFlightParams;

            UpdateUI();
        }

        private void SelectPath(int index)
        {
            _selectedPath = index;
            for (int i = 0; i < _pathButtons.Length; i++)
                _pathButtons[i]?.EnableInClassList("path-active", i == index);

            _demo.SelectPath(index);
            ClearCharts();
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
