using FPT.Core;
using RosMessageTypes.Sensor;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;

namespace FPT.Communication
{
    /// <summary>
    /// ROS2 消息 ↔ FPT 数据类型转换
    /// 后续扩展：新增话题时在此添加映射方法
    /// </summary>
    public static class Ros2MessageMapper
    {
        /// <summary> JointStateMsg.position → 关节角度（度）</summary>
        public static double[] ToJointAngles(JointStateMsg msg) => msg.position;

        /// <summary> JointStateMsg.velocity → 关节速度（度/秒）</summary>
        public static double[] ToJointVelocities(JointStateMsg msg) => msg.velocity;

        /// <summary> JointStateMsg.effort → 关节力矩（Nm）</summary>
        public static double[] ToJointTorques(JointStateMsg msg) => msg.effort;

        /// <summary> FPT 关节角度 → JointStateMsg（用于发布控制指令）</summary>
        public static JointStateMsg CreateJointCommand(double[] angles)
        {
            return new JointStateMsg
            {
                header = new HeaderMsg(),
                name = new[] { "joint1", "joint2", "joint3", "joint4", "joint5", "joint6" },
                position = angles,
                velocity = System.Array.Empty<double>(),
                effort = System.Array.Empty<double>(),
            };
        }

        /// <summary> PoseStampedMsg → DevicePose（后续 /ee_pose 话题用）</summary>
        public static DevicePose ToDevicePose(PoseStampedMsg msg)
        {
            var p = msg.pose.position;
            var o = msg.pose.orientation;
            var rpy = QuaternionToEuler(o.x, o.y, o.z, o.w);
            return new DevicePose((float)p.x, (float)p.y, (float)p.z, rpy.x, rpy.y, rpy.z);
        }

        /// <summary> DevicePose → PoseStampedMsg（后续发布末端位姿指令用）</summary>
        public static PoseStampedMsg CreatePoseCommand(DevicePose pose, string frameId = "base_link")
        {
            var euler = EulerToQuaternion(pose.Roll, pose.Pitch, pose.Yaw);
            return new PoseStampedMsg
            {
                header = new HeaderMsg { frame_id = frameId },
                pose = new RosMessageTypes.Geometry.PoseMsg
                {
                    position = new PointMsg { x = pose.X, y = pose.Y, z = pose.Z },
                    orientation = new QuaternionMsg { x = euler.x, y = euler.y, z = euler.z, w = euler.w },
                },
            };
        }

        private static UnityEngine.Vector3 QuaternionToEuler(double x, double y, double z, double w)
        {
            var q = new UnityEngine.Quaternion((float)x, (float)y, (float)z, (float)w);
            return q.eulerAngles;
        }

        private static (float x, float y, float z, float w) EulerToQuaternion(float roll, float pitch, float yaw)
        {
            var q = UnityEngine.Quaternion.Euler(roll, pitch, yaw);
            return (q.x, q.y, q.z, q.w);
        }
    }
}
