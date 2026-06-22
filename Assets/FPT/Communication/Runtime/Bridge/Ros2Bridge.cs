using System;
using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

namespace FPT.Communication
{
    /// <summary>
    /// ROS2 桥接 — 包装 ROSConnection，提供 FPT 风格的订阅/发布接口
    /// 不实现 ITransport：ROS2 是完整协议栈，不是简单字节传输
    /// </summary>
    public class Ros2Bridge
    {
        private readonly ROSConnection _ros;

        public bool IsConnected => _ros != null && _ros.HasConnectionThread;

        public Ros2Bridge()
        {
            _ros = ROSConnection.GetOrCreateInstance();
        }

        /// <summary> 连接 ROS2（阻塞直到连接成功或超时）</summary>
        public void Connect(string host = "127.0.0.1", int port = 10000)
        {
            _ros.RosIPAddress = host;
            _ros.RosPort = port;
            _ros.Connect();
            Debug.Log($"[Ros2Bridge] 正在连接 {host}:{port}...");
        }

        /// <summary> 通用订阅 </summary>
        public void Subscribe<T>(string topic, Action<T> callback) where T : Unity.Robotics.ROSTCPConnector.MessageGeneration.Message
        {
            _ros.Subscribe(topic, callback);
            Debug.Log($"[Ros2Bridge] 订阅话题: {topic}");
        }

        /// <summary> 注册发布者 — 纯发布话题（无订阅）需先注册才能 Publish </summary>
        public void RegisterPublisher<T>(string topic) where T : Unity.Robotics.ROSTCPConnector.MessageGeneration.Message
        {
            _ros.RegisterPublisher<T>(topic);
            Debug.Log($"[Ros2Bridge] 注册发布: {topic}");
        }

        /// <summary> 通用发布 </summary>
        public void Publish<T>(string topic, T message) where T : Unity.Robotics.ROSTCPConnector.MessageGeneration.Message
        {
            _ros.Publish(topic, message);
        }

        /// <summary>
        /// 订阅 /joint_states → 始终带关节名 + rad→deg 转换
        /// 回调参数: (关节名[], 角度°[], 速度[], 力矩[])
        /// </summary>
        public void SubscribeJointStates(Action<string[], double[], double[], double[]> onData)
        {
            Subscribe<JointStateMsg>("/joint_states", msg =>
            {
                var posDeg = RadToDeg(msg.position);
                onData?.Invoke(msg.name, posDeg, msg.velocity, msg.effort);
            });
        }

        /// <summary> 发布关节角度指令 — 输入为度，内部转为弧度 </summary>
        public void PublishJointCommand(double[] anglesDeg)
        {
            var anglesRad = new double[anglesDeg.Length];
            for (int i = 0; i < anglesRad.Length; i++)
                anglesRad[i] = anglesDeg[i] * Mathf.Deg2Rad;
            var msg = new JointStateMsg
            {
                header = new RosMessageTypes.Std.HeaderMsg(),
                name = new[] { "joint1", "joint2", "joint3", "joint4", "joint5", "joint6" },
                position = anglesRad,
                velocity = System.Array.Empty<double>(),
                effort = System.Array.Empty<double>(),
            };
            Publish("/joint_commands", msg);
        }

        /// <summary> 发布末端位姿指令 </summary>
        public void PublishEePoseCommand(FPT.Core.DevicePose pose)
        {
            var q = UnityEngine.Quaternion.Euler(pose.Roll, pose.Pitch, pose.Yaw);
            var msg = new RosMessageTypes.Geometry.PoseStampedMsg
            {
                header = new RosMessageTypes.Std.HeaderMsg(),
                pose = new RosMessageTypes.Geometry.PoseMsg
                {
                    position = new RosMessageTypes.Geometry.PointMsg { x = pose.X, y = pose.Y, z = pose.Z },
                    orientation = new RosMessageTypes.Geometry.QuaternionMsg { x = q.x, y = q.y, z = q.z, w = q.w },
                },
            };
            Publish("/ee_pose_command", msg);
        }

        /// <summary> 发布夹爪指令 </summary>
        public void PublishGripperCommand(double opening)
        {
            Publish("/gripper_command", new RosMessageTypes.Std.Float32Msg { data = (float)opening });
        }

        /// <summary> 发布 Bool 触发信号（用于 /execute 等话题） </summary>
        public void PublishBool(string topic, bool value = true)
        {
            Publish(topic, new RosMessageTypes.Std.BoolMsg { data = value });
        }

        /// <summary> 订阅 PoseStamped 话题 </summary>
        public void SubscribePoseStamped(string topic, Action<RosMessageTypes.Geometry.PoseStampedMsg> callback)
        {
            Subscribe(topic, callback);
        }

        /// <summary> 订阅 String 话题 </summary>
        public void SubscribeString(string topic, Action<RosMessageTypes.Std.StringMsg> callback)
        {
            Subscribe(topic, callback);
        }

        private static double[] RadToDeg(double[] rad)
        {
            var deg = new double[rad.Length];
            for (int i = 0; i < rad.Length; i++) deg[i] = rad[i] * Mathf.Rad2Deg;
            return deg;
        }
    }
}
