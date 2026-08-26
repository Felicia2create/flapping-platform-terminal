using FPT.Core;
using UnityEngine;

// ROS# 消息类型（导入 RosSharp 包后生效）
// ROS# 的消息类名不带 "Msg" 后缀，命名空间也不同
using RosSharp.RosBridgeClient.MessageTypes.Sensor;
using RosSharp.RosBridgeClient.MessageTypes.Geometry;
using RosSharp.RosBridgeClient.MessageTypes.Std;

// 避免与 UnityEngine 类型冲突
using RosPose = RosSharp.RosBridgeClient.MessageTypes.Geometry.Pose;
using RosQuaternion = RosSharp.RosBridgeClient.MessageTypes.Geometry.Quaternion;
using RosVector3 = RosSharp.RosBridgeClient.MessageTypes.Geometry.Vector3;

namespace FPT.Communication
{
    /// <summary>
    /// ROS2 消息 ↔ FPT 领域类型 双向转换
    /// 统一管理 7 DOF 关节名、弧度↔度、四元数↔欧拉角
    /// 使用 ROS# (ros-sharp) 消息类型
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
        // JointState → double[] (度)
        // ═══════════════════════════════════════════

        /// <summary>
        /// JointState.position → 关节角度（度），按 JointNames 顺序重排
        /// 如果消息中 name 为空，则按位置顺序直接映射（兼容 rosbridge 返回的无名 JointState）
        /// </summary>
        public static double[] ToJointAnglesDeg(JointState msg)
        {
            var result = new double[JointCount];
            int nameCount = msg.name != null ? msg.name.Length : 0;
            int posCount = msg.position != null ? msg.position.Length : 0;
            bool hasNames = nameCount > 0;

            for (int i = 0; i < JointCount; i++)
            {
                if (hasNames)
                {
                    // 按名称查找
                    var idx = System.Array.IndexOf(msg.name, JointNames[i]);
                    if (idx >= 0 && idx < posCount)
                        result[i] = msg.position[idx] * Mathf.Rad2Deg;
                    else
                        Debug.LogWarning($"[JointTrajectoryMapper] 关节 {JointNames[i]} 不在消息中");
                }
                else
                {
                    // 无名称时按位置顺序直接映射
                    if (i < posCount)
                        result[i] = msg.position[i] * Mathf.Rad2Deg;
                    else
                        Debug.LogWarning($"[JointTrajectoryMapper] position 长度不足，索引 {i} 无数据");
                }
            }
            return result;
        }

        // ═══════════════════════════════════════════
        // double[] (度) → JointState
        // ═══════════════════════════════════════════

        /// <summary>
        /// 创建 JointState（7 DOF），关节角输入为度，内部转弧度
        /// </summary>
        public static JointState CreateJointState(double[] anglesDeg, string frameId = "")
        {
            var rad = new double[JointCount];
            for (int i = 0; i < JointCount && i < anglesDeg.Length; i++)
                rad[i] = anglesDeg[i] * Mathf.Deg2Rad;

            return new JointState
            {
                header = new Header { frame_id = frameId },
                name = JointNames,
                position = rad,
                velocity = new double[0],
                effort = new double[0],
            };
        }

        // ═══════════════════════════════════════════
        // PoseStamped → DevicePose
        // ═══════════════════════════════════════════

        /// <summary>
        /// 从 PoseStamped 提取 DevicePose（四元数 → 欧拉角）
        /// </summary>
        public static DevicePose ToDevicePose(PoseStamped msg)
        {
            var p = msg.pose.position;
            var o = msg.pose.orientation;
            var euler = QuaternionToEuler(o.x, o.y, o.z, o.w);
            return new DevicePose((float)p.x, (float)p.y, (float)p.z, euler.x, euler.y, euler.z);
        }

        /// <summary>
        /// 从 Pose 提取 DevicePose
        /// </summary>
        public static DevicePose ToDevicePose(RosPose msg)
        {
            var o = msg.orientation;
            var euler = QuaternionToEuler(o.x, o.y, o.z, o.w);
            return new DevicePose((float)msg.position.x, (float)msg.position.y, (float)msg.position.z,
                                  euler.x, euler.y, euler.z);
        }

        // ═══════════════════════════════════════════
        // DevicePose → PoseStamped
        // ═══════════════════════════════════════════

        /// <summary>
        /// 创建 PoseStamped（末端位姿），欧拉角 → 四元数
        /// </summary>
        public static PoseStamped CreatePoseStamped(DevicePose pose, string frameId = "base_link")
        {
            var (x, y, z, w) = EulerToQuaternion(pose.Roll, pose.Pitch, pose.Yaw);
            return new PoseStamped
            {
                header = new Header { frame_id = frameId },
                pose = new RosPose
                {
                    position = new Point { x = pose.X, y = pose.Y, z = pose.Z },
                    orientation = new RosQuaternion { x = x, y = y, z = z, w = w },
                },
            };
        }

        // ═══════════════════════════════════════════
        // 四元数 ↔ 欧拉角
        // ═══════════════════════════════════════════

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
