using System;
using FPT.Business;
using FPT.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace FPT.UI
{
    /// <summary>
    /// 控制面板控制器 — 接入 InputTerminal 作为数据中心
    /// 关节滑块 / EE 输入 → InputTerminal → ROS2 FK/IK → 回显 → GhostArm 预览 → 确认执行
    /// </summary>
    public class ControlPanelController : IDisposable
    {
        private readonly InputTerminal _terminal;
        private readonly RobotArmDriver _armDriver;

        // 6 个关节的 Slider + FloatField (J1-J6)
        private readonly (Slider slider, FloatField input)[] _joints = new (Slider, FloatField)[6];

        // 末端输入
        private readonly FloatField _eeX, _eeY, _eeZ, _eeRoll, _eePitch, _eeYaw;
        private readonly DropdownField _frameSelector;

        // 操作按钮
        private readonly Button _confirmBtn, _cancelBtn, _stopBtn;
        private readonly Label _modeHint, _planStatus;

        // 夹爪
        private readonly Button _gripperClose, _gripperOpen;

        // 去抖
        private float _lastEeChangeTime;
        private const float EeDebounceS = 0.3f;
        private bool _eePending;
        private DevicePose _pendingEePose;

        public ControlPanelController(VisualElement root, InputTerminal terminal, RobotArmDriver armDriver)
        {
            _terminal = terminal;
            _armDriver = armDriver;

            // ── 关节滑块 (J1-J6) ──
            for (int i = 0; i < 6; i++)
            {
                var slider = root.Q<Slider>($"JSlider{i}");
                var input = root.Q<FloatField>($"JValue{i}");
                _joints[i] = (slider, input);

                if (slider != null && input != null)
                {
                    var idx = i;
                    slider.RegisterValueChangedCallback(evt =>
                    {
                        input.SetValueWithoutNotify(evt.newValue);
                        OnJointSliderChanged();
                    });
                    input.RegisterValueChangedCallback(evt =>
                    {
                        // 超限时自动 clamp 到边界值
                        float clamped = Math.Max((float)InputTerminal.ArmJointMinDeg[idx],
                                         Math.Min((float)InputTerminal.ArmJointMaxDeg[idx], evt.newValue));
                        if (!Mathf.Approximately(clamped, evt.newValue))
                            input.SetValueWithoutNotify(clamped);
                        slider.SetValueWithoutNotify(clamped);
                        OnJointSliderChanged();
                    });
                }
            }

            // ── 末端位姿 ──
            _eeX = root.Q<FloatField>("EeInputX");
            _eeY = root.Q<FloatField>("EeInputY");
            _eeZ = root.Q<FloatField>("EeInputZ");
            _eeRoll = root.Q<FloatField>("EeInputRoll");
            _eePitch = root.Q<FloatField>("EeInputPitch");
            _eeYaw = root.Q<FloatField>("EeInputYaw");
            _frameSelector = root.Q<DropdownField>("FrameSelector");

            // EE 字段变化 → 标记待发送（去抖）
            BindEeField(_eeX); BindEeField(_eeY); BindEeField(_eeZ);
            BindEeField(_eeRoll); BindEeField(_eePitch); BindEeField(_eeYaw);

            if (_frameSelector != null)
                _frameSelector.RegisterValueChangedCallback(evt => _terminal.SetReferenceFrame(evt.newValue));

            // ── 操作按钮 ──
            _confirmBtn = root.Q<Button>("ConfirmButton");
            _cancelBtn = root.Q<Button>("CancelButton");
            _stopBtn = root.Q<Button>("StopButton");
            _modeHint = root.Q<Label>("ModeHintLabel");
            _planStatus = root.Q<Label>("PlanStatusLabel");

            if (_confirmBtn != null) _confirmBtn.clicked += () => _terminal.ConfirmExecute();
            if (_cancelBtn != null) _cancelBtn.clicked += () =>
            {
                _terminal.Cancel();
                // 恢复 GhostArm 到实时关节角
                SyncFromRealState();
            };
            if (_stopBtn != null) _stopBtn.clicked += () =>
                _armDriver.ExecuteCommand(new StopCommand(_armDriver.DeviceId, true));

            // ── 夹爪（保持不变） ──
            _gripperClose = root.Q<Button>("GripperCloseButton");
            _gripperOpen = root.Q<Button>("GripperOpenButton");
            if (_gripperClose != null) _gripperClose.clicked += () =>
                _armDriver.ExecuteCommand(new GripperCommand(_armDriver.DeviceId, 0));
            if (_gripperOpen != null) _gripperOpen.clicked += () =>
                _armDriver.ExecuteCommand(new GripperCommand(_armDriver.DeviceId, 1.0));

            // ── 订阅终端事件 → UI 回显 ──
            _terminal.OnJointAnglesChanged += OnTerminalJointAngles;
            _terminal.OnEePoseChanged += OnTerminalEePose;
            _terminal.OnStatusChanged += OnTerminalStatus;

            // 初始同步
            SyncFromRealState();
        }

        // ═══════════════════════════════════════════
        // 滑块 → InputTerminal
        // ═══════════════════════════════════════════

        private void OnJointSliderChanged()
        {
            // 收集 7 个关节角：plate_joint 取实时值，joint1-6 取滑块值
            var angles = new double[7];
            angles[0] = GetRealTurntableAngle();  // plate_joint
            for (int i = 0; i < 6; i++)
                angles[i + 1] = _joints[i].slider?.value ?? 0;
            _terminal.SetJointAngles(angles);
        }

        private double GetRealTurntableAngle()
        {
            if (_armDriver?.CurrentState is RobotArmState state)
            {
                var names = state.JointNames;
                var angles = state.JointAngles;
                for (int i = 0; i < names.Length && i < angles.Length; i++)
                    if (names[i].Contains("plate"))
                        return angles[i];
            }
            return 0;
        }

        // ═══════════════════════════════════════════
        // EE 字段 → InputTerminal（去抖）
        // ═══════════════════════════════════════════

        private void BindEeField(FloatField field)
        {
            if (field == null) return;
            field.RegisterValueChangedCallback(_ => ScheduleEeSend());
        }

        private void ScheduleEeSend()
        {
            _pendingEePose = new DevicePose(
                _eeX?.value ?? 0, _eeY?.value ?? 0, _eeZ?.value ?? 0,
                _eeRoll?.value ?? 0, _eePitch?.value ?? 0, _eeYaw?.value ?? 0);
            _lastEeChangeTime = Time.time;
            _eePending = true;
        }

        /// <summary> 由 MainViewController.Update 调用，去抖后发送 </summary>
        public void UpdateEeDebounce()
        {
            if (!_eePending) return;
            if (Time.time - _lastEeChangeTime < EeDebounceS) return;
            _eePending = false;
            _terminal.SetEndEffectorPose(_pendingEePose);
        }

        // ═══════════════════════════════════════════
        // 终端 → UI 回显
        // ═══════════════════════════════════════════

        private void OnTerminalJointAngles(double[] angles)
        {
            // IK 返回 → 更新滑块显示（仅 joint1-6，不含 plate_joint）
            for (int i = 0; i < 6 && i + 1 < angles.Length; i++)
            {
                var val = (float)angles[i + 1];
                if (_joints[i].slider != null) _joints[i].slider.SetValueWithoutNotify(val);
                if (_joints[i].input != null) _joints[i].input.SetValueWithoutNotify(val);
            }
        }

        private void OnTerminalEePose(DevicePose pose)
        {
            // FK 返回 → 更新 EE 字段
            if (_eeX != null) _eeX.SetValueWithoutNotify(pose.X);
            if (_eeY != null) _eeY.SetValueWithoutNotify(pose.Y);
            if (_eeZ != null) _eeZ.SetValueWithoutNotify(pose.Z);
            if (_eeRoll != null) _eeRoll.SetValueWithoutNotify(pose.Roll);
            if (_eePitch != null) _eePitch.SetValueWithoutNotify(pose.Pitch);
            if (_eeYaw != null) _eeYaw.SetValueWithoutNotify(pose.Yaw);
        }

        private void OnTerminalStatus(string status)
        {
            if (_planStatus != null)
            {
                _planStatus.text = status switch
                {
                    "planning" => "正在规划...",
                    "success" => "规划成功 ✓",
                    "failed" => "规划失败 ✗",
                    "executed" => "已执行",
                    "cancelled" => "已取消",
                    _ => ""
                };
                _planStatus.RemoveFromClassList("planning");
                _planStatus.RemoveFromClassList("failed");
                if (status == "planning") _planStatus.AddToClassList("planning");
                if (status == "failed") _planStatus.AddToClassList("failed");
            }
        }

        /// <summary> 由外部每帧调用，更新模式提示 </summary>
        public void UpdateModeHint()
        {
            if (_modeHint == null) return;
            _modeHint.text = _terminal.ActiveMode == ControlMode.JointSpace
                ? "模式: 关节空间"
                : "模式: 笛卡尔空间";
            _modeHint.style.color = _terminal.ActiveMode == ControlMode.JointSpace
                ? new StyleColor(new Color(1f, 0.65f, 0.2f))   // 橙色
                : new StyleColor(new Color(0.3f, 0.65f, 1f));   // 蓝色
        }

        // ═══════════════════════════════════════════
        // 同步
        // ═══════════════════════════════════════════

        private void SyncFromRealState()
        {
            if (_armDriver?.CurrentState is RobotArmState state && state.JointAngles.Length >= 7)
            {
                _terminal.SyncFromRealState(state.JointAngles);
            }
        }

        public void Dispose()
        {
            if (_terminal != null)
            {
                _terminal.OnJointAnglesChanged -= OnTerminalJointAngles;
                _terminal.OnEePoseChanged -= OnTerminalEePose;
                _terminal.OnStatusChanged -= OnTerminalStatus;
            }
        }
    }
}
