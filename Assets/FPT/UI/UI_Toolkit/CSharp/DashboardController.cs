using System;
using FPT.Business;
using FPT.Core;
using UnityEngine.UIElements;

namespace FPT.UI
{
    /// <summary>
    /// 仪表盘控制器 — 订阅 ArmDriver 实时数据 + InputTerminal 目标数据
    /// </summary>
    public class DashboardController : IDisposable
    {
        private readonly RobotArmDriver _armDriver;
        private readonly InputTerminal _terminal;

        // 实时数据
        private readonly Label[] _jointLabels = new Label[6];
        private readonly Label _turntableAngle;
        private readonly Label _posX, _posY, _posZ;
        private readonly Label _rotR, _rotP, _rotY;

        // 目标数据（预览）
        private readonly Label[] _targetJointLabels = new Label[6];
        private readonly Label _targetPosX, _targetPosY, _targetPosZ;
        private readonly Label _targetRotR, _targetRotP, _targetRotY;

        public DashboardController(VisualElement root, RobotArmDriver armDriver, InputTerminal terminal)
        {
            _armDriver = armDriver;
            _terminal = terminal;

            // 实时关节
            for (int i = 0; i < 6; i++)
                _jointLabels[i] = root.Q<Label>($"JLabel{i}");
            _turntableAngle = root.Q<Label>("TurntableAngle");

            // 实时位姿
            _posX = root.Q<Label>("PosXLabel");
            _posY = root.Q<Label>("PosYLabel");
            _posZ = root.Q<Label>("PosZLabel");
            _rotR = root.Q<Label>("RotRLabel");
            _rotP = root.Q<Label>("RotPLabel");
            _rotY = root.Q<Label>("RotYLabel");

            // 目标关节
            for (int i = 0; i < 6; i++)
                _targetJointLabels[i] = root.Q<Label>($"TargetJLabel{i}");

            // 目标位姿
            _targetPosX = root.Q<Label>("TargetPosX");
            _targetPosY = root.Q<Label>("TargetPosY");
            _targetPosZ = root.Q<Label>("TargetPosZ");
            _targetRotR = root.Q<Label>("TargetRotR");
            _targetRotP = root.Q<Label>("TargetRotP");
            _targetRotY = root.Q<Label>("TargetRotY");

            // 订阅
            _armDriver.OnStateChanged += OnArmStateChanged;
            if (_terminal != null)
            {
                _terminal.OnJointAnglesChanged += OnTargetJointAngles;
                _terminal.OnEePoseChanged += OnTargetEePose;
            }
        }

        private static int JointIndexFromName(string name)
        {
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
            if (state is not RobotArmState arm) return;

            for (int i = 0; i < arm.JointAngles.Length && i < arm.JointNames.Length; i++)
            {
                var idx = JointIndexFromName(arm.JointNames[i]);
                if (idx >= 0 && idx < 6 && _jointLabels[idx] != null)
                    _jointLabels[idx].text = $"Joint{idx + 1}: {arm.JointAngles[i]:F1}°";
            }

            // 转台
            if (_turntableAngle != null)
            {
                var plateIdx = System.Array.FindIndex(arm.JointNames, n => n.Contains("plate"));
                if (plateIdx >= 0)
                    _turntableAngle.text = $"转台: {arm.JointAngles[plateIdx]:F1}°";
            }

            var p = arm.EndEffectorPose;
            SetLabel(_posX, $"X: {p.X:F3}");
            SetLabel(_posY, $"Y: {p.Y:F3}");
            SetLabel(_posZ, $"Z: {p.Z:F3}");
            SetLabel(_rotR, $"R: {p.Roll:F1}");
            SetLabel(_rotP, $"P: {p.Pitch:F1}");
            SetLabel(_rotY, $"Y: {p.Yaw:F1}");
        }

        private void OnTargetJointAngles(double[] angles)
        {
            for (int i = 0; i < 6 && i + 1 < angles.Length; i++)
            {
                if (_targetJointLabels[i] != null)
                    _targetJointLabels[i].text = $"Joint{i + 1}: {angles[i + 1]:F1}°";
            }
        }

        private void OnTargetEePose(DevicePose pose)
        {
            SetLabel(_targetPosX, $"X: {pose.X:F3}");
            SetLabel(_targetPosY, $"Y: {pose.Y:F3}");
            SetLabel(_targetPosZ, $"Z: {pose.Z:F3}");
            SetLabel(_targetRotR, $"R: {pose.Roll:F1}");
            SetLabel(_targetRotP, $"P: {pose.Pitch:F1}");
            SetLabel(_targetRotY, $"Y: {pose.Yaw:F1}");
        }

        private static void SetLabel(Label label, string text)
        {
            if (label != null) label.text = text;
        }

        public void Dispose()
        {
            _armDriver.OnStateChanged -= OnArmStateChanged;
            if (_terminal != null)
            {
                _terminal.OnJointAnglesChanged -= OnTargetJointAngles;
                _terminal.OnEePoseChanged -= OnTargetEePose;
            }
        }
    }
}
