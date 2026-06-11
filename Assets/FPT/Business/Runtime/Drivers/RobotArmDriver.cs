using System;
using System.Threading.Tasks;
using FPT.Communication;
using FPT.Core;
using UnityEngine;

namespace FPT.Business
{
    public class RobotArmDriver : DeviceDriverBase
    {
        private Ros2Bridge _ros2;
        private RobotArmState _state;

        public override string DeviceId { get; }
        public override DeviceInfo Info { get; }
        public override IDeviceState CurrentState => _state;

        public override System.Collections.Generic.IReadOnlyList<Type> SupportedCommandTypes { get; }
            = new[] { typeof(JointCommand), typeof(EePoseCommand), typeof(GripperCommand),
                      typeof(StopCommand), typeof(HomeCommand), typeof(SetModeCommand) };

        public RobotArmDriver(string deviceId, DeviceInfo info = null)
        {
            DeviceId = deviceId;
            Info = info ?? new DeviceInfo { DeviceId = deviceId, DisplayName = "机械臂", Type = FPT.Core.DeviceType.RobotArm };
            _state = new RobotArmState { DeviceId = deviceId, Connection = DeviceConnectionState.Disconnected };
        }

        public void Bind(Ros2Bridge ros2)
        {
            _ros2 = ros2;
            _ros2.SubscribeJointStates((names, angles, vels, torques) =>
            {
                _state.JointNames = names;
                _state.JointAngles = angles;
                _state.JointVelocities = vels;
                _state.JointTorques = torques;
                _state.LastUpdateTime = DateTime.Now;
                _state.Connection = DeviceConnectionState.Operational;
                NotifyStateChanged(_state);
            });
            Debug.Log($"[RobotArm:{DeviceId}] ROS2 话题订阅完成");
        }

        protected override void ConfigurePipeline(CommandPipeline pipeline)
        {
            pipeline
                .AddInterceptor(new JointLimitInterceptor(
                    new double[] { -170, -120, -170, -120, -170, -120 },
                    new double[] { 170, 120, 170, 120, 170, 120 }))
                .AddInterceptor(new SpeedLimitInterceptor(180, 2.0));
        }

        protected override Task<CommandResult> SendCommandToDevice(IDeviceCommand command)
        {
            if (_ros2 == null || !_ros2.IsConnected)
                return Task.FromResult(CommandResult.Fail("ROS2 未连接"));

            switch (command)
            {
                case JointCommand jc:
                    _ros2.PublishJointCommand(jc.TargetAngles);
                    return Task.FromResult(CommandResult.Ok("关节指令已发布"));
                case EePoseCommand ep:
                    _ros2.PublishEePoseCommand(ep.TargetPose);
                    return Task.FromResult(CommandResult.Ok("末端位姿指令已发布"));
                case GripperCommand gc:
                    _ros2.PublishGripperCommand(gc.Opening);
                    return Task.FromResult(CommandResult.Ok("夹爪指令已发布"));
                case StopCommand _:
                    _ros2.PublishJointCommand(_state.JointAngles ?? new double[6]);
                    return Task.FromResult(CommandResult.Ok("停止"));
                default:
                    return Task.FromResult(CommandResult.Fail($"不支持的指令: {command.CommandType}"));
            }
        }

        public override Task InitializeAsync(IDeviceChannel channel) => Task.CompletedTask;
    }
}
