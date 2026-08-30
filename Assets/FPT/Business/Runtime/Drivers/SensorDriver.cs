using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FPT.Communication;
using FPT.Core;
using RosSharp.RosBridgeClient.MessageTypes.Sensor;
using RosSharp.RosBridgeClient.MessageTypes.SensorInterface;
using UnityEngine;

namespace FPT.Business
{
    /// <summary>
    /// 传感器驱动 — 订阅 ROS2 topic，将所有可展示数据汇入 SensorState.Readings。
    ///
    /// 职责：纯数据采集，不做控制。与 RobotArmDriver 订阅相同 topic 不冲突。
    /// 扩展方式：在 Bind() 中添加 Subscribe 调用，UI 自动识别新通道。
    ///
    /// 键名约定："分类/名称"（如 "关节角度/joint1"、"力传感器/Fx"），
    /// ChannelRegistry 按 "/" 前缀自动分组。
    /// </summary>
    public class SensorDriver : DeviceDriverBase
    {
        private Ros2Node _node;
        private SensorState _state;

        public override string DeviceId { get; }
        public override DeviceInfo Info { get; }
        public override IDeviceState CurrentState => _state;

        // 传感器只读，不支持任何指令
        public override IReadOnlyList<Type> SupportedCommandTypes { get; }
            = Array.Empty<Type>();

        public SensorDriver(string deviceId, DeviceInfo info = null)
        {
            DeviceId = deviceId;
            Info = info ?? new DeviceInfo
            {
                DeviceId = deviceId,
                DisplayName = "传感器",
                Type = FPT.Core.DeviceType.SensorModule
            };
            _state = new SensorState
            {
                DeviceId = deviceId,
                Connection = DeviceConnectionState.Disconnected
            };
        }

        /// <summary>
        /// 绑定 Ros2Node — 订阅所有数据 topic。
        /// 添加新传感器时，在此方法中加一个 Subscribe 即可。
        /// </summary>
        public void Bind(Ros2Node node)
        {
            _node = node;

            // ── 预注册所有通道（默认值0，数据到达后自动更新）──
            var names = JointTrajectoryMapper.JointNames;
            foreach (var name in names)
            {
                _state.Readings[$"关节角度/{name}"] = 0;
                _state.Readings[$"关节速度/{name}"] = 0;
                _state.Readings[$"关节力矩/{name}"] = 0;
            }
            _state.Readings["末端位置/X"] = 0;
            _state.Readings["末端位置/Y"] = 0;
            _state.Readings["末端位置/Z"] = 0;
            _state.Readings["末端姿态/R"] = 0;
            _state.Readings["末端姿态/P"] = 0;
            _state.Readings["末端姿态/Y"] = 0;
            _state.Readings["六维力/Fx"] = 0;
            _state.Readings["六维力/Fy"] = 0;
            _state.Readings["六维力/Fz"] = 0;
            _state.Readings["六维力/Mx"] = 0;
            _state.Readings["六维力/My"] = 0;
            _state.Readings["六维力/Mz"] = 0;
            NotifyStateChanged(_state);

            // ── 关节状态（角度 / 速度 / 力矩）──
            _node.Subscribe<JointState>("/joint_states", msg =>
            {
                var names = JointTrajectoryMapper.JointNames;
                var angles = JointTrajectoryMapper.ToJointAnglesDeg(msg);

                for (int i = 0; i < names.Length; i++)
                {
                    if (i < angles.Length)
                        UpdateReading($"关节角度/{names[i]}", angles[i]);
                    if (i < msg.velocity.Length)
                        UpdateReading($"关节速度/{names[i]}", msg.velocity[i]);
                    if (i < msg.effort.Length)
                        UpdateReading($"关节力矩/{names[i]}", msg.effort[i]);
                }
            });

            // ═══════════════════════════════════════════
            //  睿尔曼实机传感器
            // ═══════════════════════════════════════════

            // 末端位姿（欧拉角）
            _node.Subscribe<Jointposeeuler>("/rm_driver/udp_joint_pose_euler", msg =>
            {
                if (msg.position != null && msg.position.Length >= 3)
                {
                    UpdateReading("末端位置/X", msg.position[0]);
                    UpdateReading("末端位置/Y", msg.position[1]);
                    UpdateReading("末端位置/Z", msg.position[2]);
                }
                if (msg.euler != null && msg.euler.Length >= 3)
                {
                    // 弧度 → 角度
                    UpdateReading("末端姿态/R", msg.euler[0] * Mathf.Rad2Deg);
                    UpdateReading("末端姿态/P", msg.euler[1] * Mathf.Rad2Deg);
                    UpdateReading("末端姿态/Y", msg.euler[2] * Mathf.Rad2Deg);
                }
            });

            // 六维力传感器（外受力）
            _node.Subscribe<Sixforce>("/rm_driver/udp_six_zero_force", msg =>
            {
                UpdateReading("六维力/Fx", msg.force_fx);
                UpdateReading("六维力/Fy", msg.force_fy);
                UpdateReading("六维力/Fz", msg.force_fz);
                UpdateReading("六维力/Mx", msg.force_mx);
                UpdateReading("六维力/My", msg.force_my);
                UpdateReading("六维力/Mz", msg.force_mz);
            });

            // 关节速度
            _node.Subscribe<Jointspeed>("/rm_driver/udp_joint_speed", msg =>
            {
                if (msg.joint_speed == null) return;
                var names = JointTrajectoryMapper.JointNames;
                for (int i = 0; i < msg.joint_speed.Length; i++)
                {
                    string name = i < names.Length ? names[i] : $"joint{i + 1}";
                    UpdateReading($"关节速度/{name}", msg.joint_speed[i]);
                }
            });

            Debug.Log($"[SensorDriver:{DeviceId}] 已绑定 ROS2 topics");
        }

        /// <summary> 更新一个读数通道 </summary>
        private void UpdateReading(string key, double value)
        {
            _state.Readings[key] = value;
            _state.LastUpdateTime = DateTime.Now;
            _state.Connection = DeviceConnectionState.Operational;
            NotifyStateChanged(_state);
        }

        protected override Task<CommandResult> SendCommandToDevice(IDeviceCommand command)
            => Task.FromResult(CommandResult.Fail("传感器设备不支持指令"));

        protected override void ConfigurePipeline(CommandPipeline pipeline) { }
    }
}
