using RosMessageTypes.Sensor;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using FPT.Core;
using UnityEngine;

namespace FPT.Communication
{
    /// <summary>
    /// ROS2 消息 ↔ FPT 领域类型 双向转换
    /// 统一管理 7 DOF 关节名、弧度↔度、四元数↔欧拉角
    /// </summary>
    public static class JointTrajectoryMapper
    {
        /// <summary>
        /// 7 DOF 关节名（固定顺序，与 ROS URDF / Unity PlatformJointController 一致）
        /// </summary>
        public static readonly string[] JointNames = new[]
        {
            "plate_joint", "joint1", "joint2", "joint3", "joint4", "joint5", "joint6"
        };

        public const int JointCount = 7;

        // ═══════════════════════════════════════════
        // JointStateMsg → double[] (度)
        // ═══════════════════════════════════════════

        /// <summary>
        /// JointStateMsg.position → 关节角度（度），按 JointNames 顺序重排
        /// msg 中各关节可能出现顺序不同，此方法按名称对齐
        /// </summary>
        public static double[] ToJointAnglesDeg(JointStateMsg msg)
        {
            var result = new double[JointCount];
            for (int i = 0; i < JointCount; i++)
            {
                var idx = System.Array.IndexOf(msg.name, JointNames[i]);
                if (idx >= 0 && idx < msg.position.Length)
                    result[i] = msg.position[idx] * Mathf.Rad2Deg;
                else
                    Debug.LogWarning($"[JointTrajectoryMapper] 关节 {JointNames[i]} 不在消息中");
            }
            return result;
        }

        /// <summary>
        /// 直接从消息提取（不重排），返回 position 数组转为度
        /// </summary>
        public static double[] ToDegreesRaw(JointStateMsg msg)
        {
            var deg = new double[msg.position.Length];
            for (int i = 0; i < deg.Length; i++)
                deg[i] = msg.position[i] * Mathf.Rad2Deg;
            return deg;
        }

        // ═══════════════════════════════════════════
        // double[] (度) → JointStateMsg
        // ═══════════════════════════════════════════

        /// <summary>
        /// 创建 JointStateMsg（7 DOF，含转台），关节角输入为度，内部转弧度
        /// frameId 通过 msg.header.frame_id 传递（FK 用）
        /// </summary>
        public static JointStateMsg CreateJointState(double[] anglesDeg, string frameId = "")
        {
            var rad = new double[JointCount];
            for (int i = 0; i < JointCount && i < anglesDeg.Length; i++)
                rad[i] = anglesDeg[i] * Mathf.Deg2Rad;

            return new JointStateMsg
            {
                header = new HeaderMsg { frame_id = frameId },
                name = JointNames,
                position = rad,
                velocity = System.Array.Empty<double>(),
                effort = System.Array.Empty<double>(),
            };
        }

        // ═══════════════════════════════════════════
        // PoseStampedMsg → DevicePose
        // ═══════════════════════════════════════════

        /// <summary>
        /// 从 PoseStampedMsg 提取 DevicePose（四元数 → 欧拉角）
        /// </summary>
        public static DevicePose ToDevicePose(PoseStampedMsg msg)
        {
            var p = msg.pose.position;
            var o = msg.pose.orientation;
            var euler = QuaternionToEuler(o.x, o.y, o.z, o.w);
            return new DevicePose((float)p.x, (float)p.y, (float)p.z, euler.x, euler.y, euler.z);
        }

        /// <summary>
        /// 从 PoseMsg 提取 DevicePose
        /// </summary>
        public static DevicePose ToDevicePose(PoseMsg msg)
        {
            var o = msg.orientation;
            var euler = QuaternionToEuler(o.x, o.y, o.z, o.w);
            return new DevicePose((float)msg.position.x, (float)msg.position.y, (float)msg.position.z,
                                  euler.x, euler.y, euler.z);
        }

        // ═══════════════════════════════════════════
        // DevicePose → PoseStampedMsg
        // ═══════════════════════════════════════════

        /// <summary>
        /// 创建 PoseStampedMsg（末端位姿），欧拉角 → 四元数
        /// frameId 用于传递参考坐标系
        /// </summary>
        public static PoseStampedMsg CreatePoseStamped(DevicePose pose, string frameId = "base_link")
        {
            var (x, y, z, w) = EulerToQuaternion(pose.Roll, pose.Pitch, pose.Yaw);
            return new PoseStampedMsg
            {
                header = new HeaderMsg { frame_id = frameId },
                pose = new PoseMsg
                {
                    position = new PointMsg { x = pose.X, y = pose.Y, z = pose.Z },
                    orientation = new QuaternionMsg { x = x, y = y, z = z, w = w },
                },
            };
        }

        // ═══════════════════════════════════════════
        // 四元数 ↔ 欧拉角
        // ═══════════════════════════════════════════

        private static Vector3 QuaternionToEuler(double x, double y, double z, double w)
        {
            var q = new Quaternion((float)x, (float)y, (float)z, (float)w);
            return q.eulerAngles;
        }

        private static (float x, float y, float z, float w) EulerToQuaternion(float roll, float pitch, float yaw)
        {
            var q = Quaternion.Euler(roll, pitch, yaw);
            return (q.x, q.y, q.z, q.w);
        }
    }
}
