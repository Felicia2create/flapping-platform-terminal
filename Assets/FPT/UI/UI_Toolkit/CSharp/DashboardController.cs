using System;
using FPT.Business;
using FPT.Core;
using UnityEngine.UIElements;

namespace FPT.UI
{
    /// <summary>
    /// 仪表盘控制器 — 订阅 ArmDriver 事件刷新关节数据和末端位姿
    /// </summary>
    public class DashboardController : IDisposable
    {
        private readonly RobotArmDriver _armDriver;
        private readonly Label[] _jointLabels = new Label[6];
        private readonly Label _turntableAngle;

        // 末端位姿
        private readonly Label _posX, _posY, _posZ;
        private readonly Label _rotR, _rotP, _rotY;

        public DashboardController(VisualElement root, RobotArmDriver armDriver)
        {
            _armDriver = armDriver;

            for (int i = 0; i < 6; i++)
                _jointLabels[i] = root.Q<Label>($"JLabel{i}");

            _turntableAngle = root.Q<Label>("TurntableAngle");

            _posX = root.Q<Label>("PosXLabel");
            _posY = root.Q<Label>("PosYLabel");
            _posZ = root.Q<Label>("PosZLabel");
            _rotR = root.Q<Label>("RotRLabel");
            _rotP = root.Q<Label>("RotPLabel");
            _rotY = root.Q<Label>("RotYLabel");

            _armDriver.OnStateChanged += OnArmStateChanged;
        }

        /// <summary> 从关节名提取索引: "joint3" 或 "arm1_joint3" → 2 </summary>
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
                    _jointLabels[idx].text = $"{arm.JointNames[i]}: {arm.JointAngles[i]:F1}°";
            }

            var p = arm.EndEffectorPose;
            if (_posX != null) _posX.text = $"X: {p.X:F3}";
            if (_posY != null) _posY.text = $"Y: {p.Y:F3}";
            if (_posZ != null) _posZ.text = $"Z: {p.Z:F3}";
            if (_rotR != null) _rotR.text = $"R: {p.Roll:F1}";
            if (_rotP != null) _rotP.text = $"P: {p.Pitch:F1}";
            if (_rotY != null) _rotY.text = $"Y: {p.Yaw:F1}";
        }

        public void Dispose()
        {
            _armDriver.OnStateChanged -= OnArmStateChanged;
        }
    }
}
