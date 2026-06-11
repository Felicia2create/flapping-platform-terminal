using System;
using FPT.Business;
using FPT.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace FPT.UI
{
    /// <summary>
    /// 控制面板控制器 — 按钮/滑块交互，发送指令到 Business 层
    /// </summary>
    public class ControlPanelController : IDisposable
    {
        private readonly RobotArmDriver _armDriver;

        // 6 个关节的 Slider + FloatField
        private readonly (Slider slider, FloatField input)[] _joints = new (Slider, FloatField)[6];

        // 末端输入
        private readonly FloatField _eeX, _eeY, _eeZ, _eeRoll, _eePitch, _eeYaw;
        private readonly Button _eeSendBtn;

        // 夹爪
        private readonly Slider _gripperSlider;
        private readonly Button _gripperClose, _gripperOpen;

        // 模式 + 停止
        private readonly Button _jointModeBtn, _cartesianModeBtn, _stopBtn;

        public ControlPanelController(VisualElement root, RobotArmDriver armDriver)
        {
            _armDriver = armDriver;

            // 关节滑块
            for (int i = 0; i < 6; i++)
            {
                var slider = root.Q<Slider>($"JSlider{i}");
                var input = root.Q<FloatField>($"JValue{i}");
                _joints[i] = (slider, input);

                if (slider != null && input != null)
                {
                    var idx = i;
                    slider.RegisterValueChangedCallback(evt => { input.value = evt.newValue; });
                    input.RegisterValueChangedCallback(evt => { slider.value = evt.newValue; });
                }
            }

            // 末端
            _eeX = root.Q<FloatField>("EeInputX");
            _eeY = root.Q<FloatField>("EeInputY");
            _eeZ = root.Q<FloatField>("EeInputZ");
            _eeRoll = root.Q<FloatField>("EeInputRoll");
            _eePitch = root.Q<FloatField>("EeInputPitch");
            _eeYaw = root.Q<FloatField>("EeInputYaw");
            _eeSendBtn = root.Q<Button>("EeSendButton");
            if (_eeSendBtn != null) _eeSendBtn.clicked += OnEeSend;

            // 夹爪
            _gripperClose = root.Q<Button>("GripperCloseButton");
            _gripperOpen = root.Q<Button>("GripperOpenButton");
            if (_gripperClose != null) _gripperClose.clicked += () => SendGripper(0);
            if (_gripperOpen != null) _gripperOpen.clicked += () => SendGripper(100);

            // 模式
            _jointModeBtn = root.Q<Button>("JointModeButton");
            _cartesianModeBtn = root.Q<Button>("CartesianModeButton");
            if (_jointModeBtn != null) _jointModeBtn.clicked += () => SendMode("JointSpace");
            if (_cartesianModeBtn != null) _cartesianModeBtn.clicked += () => SendMode("CartesianSpace");

            // 停止
            _stopBtn = root.Q<Button>("StopButton");
            if (_stopBtn != null) _stopBtn.clicked += SendStop;
        }

        private async void OnEeSend()
        {
            var pose = new DevicePose(
                _eeX?.value ?? 0, _eeY?.value ?? 0, _eeZ?.value ?? 0,
                _eeRoll?.value ?? 0, _eePitch?.value ?? 0, _eeYaw?.value ?? 0);
            await _armDriver.ExecuteCommand(new EePoseCommand(_armDriver.DeviceId, pose));
        }

        private async void SendGripper(double pct) =>
            await _armDriver.ExecuteCommand(new GripperCommand(_armDriver.DeviceId, pct / 100.0));

        private async void SendMode(string m) =>
            await _armDriver.ExecuteCommand(new SetModeCommand(_armDriver.DeviceId, m));

        private async void SendStop() =>
            await _armDriver.ExecuteCommand(new StopCommand(_armDriver.DeviceId, true));

        public void Dispose()
        {
            if (_eeSendBtn != null) _eeSendBtn.clicked -= OnEeSend;
            // template-based controllers no longer needed
        }
    }
}
