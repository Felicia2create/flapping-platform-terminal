using FPT.Business;
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

        private bool _isPlaying;

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

            _demo.OnArmPlayStateChanged += OnPlayStateChanged;

            UpdateUI();
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
                _demo.OnArmPlayStateChanged -= OnPlayStateChanged;
            if (_playBtn != null) _playBtn.clicked -= OnPlayClicked;
        }
    }
}
