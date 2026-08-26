using System;
using System.Threading.Tasks;
using FPT.Communication;
using FPT.Core;
using RosSharp.RosBridgeClient.MessageTypes.FptInterface;
using RosSharp.RosBridgeClient.MessageTypes.Geometry;
using RosSharp.RosBridgeClient.MessageTypes.Sensor;
using UnityEngine;

namespace FPT.Business
{
    public class RobotArmDriver : DeviceDriverBase
    {
        private Ros2Node _node;
        private RobotArmState _state;

        public override string DeviceId { get; }
        public override DeviceInfo Info { get; }
        public override IDeviceState CurrentState => _state;

        public override System.Collections.Generic.IReadOnlyList<Type> SupportedCommandTypes { get; }
            = new[] { typeof(JointCommand), typeof(EePoseCommand),
                      typeof(StopCommand), typeof(HomeCommand), typeof(SetModeCommand) };

        public RobotArmDriver(string deviceId, DeviceInfo info = null)
        {
            DeviceId = deviceId;
            Info = info ?? new DeviceInfo { DeviceId = deviceId, DisplayName = "机械臂", Type = FPT.Core.DeviceType.RobotArm };
            _state = new RobotArmState { DeviceId = deviceId, Connection = DeviceConnectionState.Disconnected };
        }

        /// <summary>
        /// 绑定 Ros2Node — 订阅 /joint_states 并注册 service
        /// </summary>
        public void Bind(Ros2Node node)
        {
            _node = node;

            // 订阅实时关节状态
            _node.Subscribe<JointState>("/joint_states", msg =>
            {
                var anglesDeg = JointTrajectoryMapper.ToJointAnglesDeg(msg);
                // 角度已按 JointTrajectoryMapper.JointNames 重排，名称也用同一顺序
                _state.JointNames = JointTrajectoryMapper.JointNames;
                _state.JointAngles = anglesDeg;
                _state.JointVelocities = msg.velocity;
                _state.JointTorques = msg.effort;
                _state.LastUpdateTime = DateTime.Now;
                _state.Connection = DeviceConnectionState.Operational;
                NotifyStateChanged(_state);
            });

            // 订阅实时末端位姿
            _node.Subscribe<PoseStamped>("/ee_pose", msg =>
            {
                _state.EndEffectorPose = JointTrajectoryMapper.ToDevicePose(msg);
                _state.LastUpdateTime = DateTime.Now;
                NotifyStateChanged(_state);
            });

            // 注册 service client（使用 ROS2 服务类型名）
            _node.RegisterService("joint_plan", JointPlanService.RosMessageName,
                JointPlanService.RequestMessageName, JointPlanService.ResponseMessageName);
            _node.RegisterService("ee_plan", EEPlanService.RosMessageName,
                EEPlanService.RequestMessageName, EEPlanService.ResponseMessageName);
            _node.RegisterService("execute", ExecuteService.RosMessageName,
                ExecuteService.RequestMessageName, ExecuteService.ResponseMessageName);

            Debug.Log($"[RobotArm:{DeviceId}] Ros2Node 绑定完成");
        }

        protected override void ConfigurePipeline(CommandPipeline pipeline)
        {
            pipeline
                .AddInterceptor(new JointLimitInterceptor(
                    new double[] { -178, -178, -178, -178, -178, -180 },
                    new double[] { 178,   178,  145,  178,  178,  180 }))
                .AddInterceptor(new SpeedLimitInterceptor(180, 2.0));
        }

        protected override async Task<CommandResult> SendCommandToDevice(IDeviceCommand command)
        {
            if (_node == null || !_node.IsConnected)
                return CommandResult.Fail("ROS2 未连接");

            try
            {
                switch (command)
                {
                    case JointCommand jc:
                        var jointMsg = JointTrajectoryMapper.CreateJointState(jc.TargetAngles, "base_link");
                        var jointReq = new JointPlanRequest(jointMsg);
                        await _node.CallServiceAsync<JointPlanResponse>("joint_plan", jointReq);
                        return CommandResult.Ok("关节规划 service 已调用");

                    case EePoseCommand ep:
                        var poseMsg = JointTrajectoryMapper.CreatePoseStamped(ep.TargetPose, "base_link");
                        var eeReq = new EEPlanRequest(poseMsg);
                        await _node.CallServiceAsync<EEPlanResponse>("ee_plan", eeReq);
                        return CommandResult.Ok("末端规划 service 已调用");

                    case StopCommand _:
                        var stopMsg = JointTrajectoryMapper.CreateJointState(_state.JointAngles ?? new double[7], "base_link");
                        var stopReq = new JointPlanRequest(stopMsg);
                        await _node.CallServiceAsync<JointPlanResponse>("joint_plan", stopReq);
                        return CommandResult.Ok("停止");

                    default:
                        return CommandResult.Fail($"不支持的指令: {command.CommandType}");
                }
            }
            catch (Exception ex)
            {
                return CommandResult.Fail($"Service 调用失败: {ex.Message}");
            }
        }

        public override Task InitializeAsync(IDeviceChannel channel) => Task.CompletedTask;
    }
}
