using System.Collections.Generic;
using FPT.Communication;
using UnityEngine;

namespace FPT.Visualization
{
    /// <summary>
    /// 平台关节控制器 — 桥接 ArticulationBody 和 FPT 业务层
    ///
    /// 关节映射（ROS 顺序）:
    ///   joint1 ~ joint6 = 机械臂 6 轴
    ///   plate_joint      = 转台旋转
    ///
    /// 使用 ArticulationBody.xDrive.target 驱动关节角度（度）
    /// </summary>
    public class PlatformJointController : MonoBehaviour
    {
        [Header("目标预制体根节点")]
        [SerializeField] private GameObject _platformRoot;

        [Header("关节名称（与 ROS joint_states 顺序一致）")]
        [SerializeField] private string[] _jointNames = new[]
        {
            "joint1", "joint2", "joint3", "joint4", "joint5", "joint6", "plate_joint"
        };

        // Arm joints (index 0-5) + Turntable (index 6)
        private readonly Dictionary<string, ArticulationBody> _joints
            = new Dictionary<string, ArticulationBody>();

        private readonly Dictionary<string, float> _targetAngles
            = new Dictionary<string, float>();

        public int JointCount => _joints.Count;
        public bool IsReady => _joints.Count > 0;

        private void Start()
        {
            DiscoverJoints();

            // 自动绑定 Ros2Bridge（等待 AppContext 就绪）
            var ctx = FPT.Business.AppContext.Instance;
            if (ctx != null && ctx.Ros2Bridge != null)
            {
                Bind(ctx.Ros2Bridge);
                Debug.Log($"[PlatformJointController] 已绑定 Ros2Bridge，{_joints.Count} 个关节就绪");
            }
            else
            {
                Debug.LogWarning("[PlatformJointController] AppContext 或 Ros2Bridge 未就绪");
            }
        }

        /// <summary>
        /// 自动发现所有 ArticulationBody 关节
        /// </summary>
        [ContextMenu("Discover Joints")]
        public void DiscoverJoints()
        {
            _joints.Clear();

            if (_platformRoot == null)
                _platformRoot = gameObject;

            var bodies = _platformRoot.GetComponentsInChildren<ArticulationBody>();

            foreach (var body in bodies)
            {
                // 尝试通过关节名匹配
                foreach (var name in _jointNames)
                {
                    if (body.name.Contains(name) ||
                        body.jointPosition.dofCount > 0)
                    {
                        // ArticulationBody 的 name 不直接是关节名
                        // 我们通过遍历所有 body 并按其父级 GameObjects 匹配
                        break;
                    }
                }
            }

            // 更可靠的方式：按 GameObject 名称查找
            // plate_joint → platform_plate_Link 上的 ArticulationBody
            // joint1~6 → arm1_link1~6 上的 ArticulationBody
            foreach (var kv in GetJointGameObjectMapping())
            {
                var found = false;
                foreach (var body in bodies)
                {
                    if (body.gameObject.name == kv.Value)
                    {
                        _joints[kv.Key] = body;
                        _targetAngles[kv.Key] = (float)GetCurrentAngle(body);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    Debug.LogWarning($"[PlatformJointController] 未找到关节 {kv.Key} (GameObject: {kv.Value})");
            }

            Debug.Log($"[PlatformJointController] 发现 {_joints.Count} 个关节: {string.Join(", ", _joints.Keys)}");
        }

        /// <summary>
        /// ROS 关节名 → GameObject 名映射
        /// </summary>
        private Dictionary<string, string> GetJointGameObjectMapping()
        {
            return new Dictionary<string, string>
            {
                { "joint1",       "arm1_link1" },
                { "joint2",       "arm1_link2" },
                { "joint3",       "arm1_link3" },
                { "joint4",       "arm1_link4" },
                { "joint5",       "arm1_link5" },
                { "joint6",       "arm1_link6" },
                { "plate_joint",  "platform_plate_Link" },
            };
        }

        /// <summary>
        /// 设置单个关节角度（度）
        /// </summary>
        public void SetJointAngle(string jointName, float angleDegrees)
        {
            if (!_joints.TryGetValue(jointName, out var body))
            {
                Debug.LogWarning($"[PlatformJointController] 关节不存在: {jointName}");
                return;
            }

            _targetAngles[jointName] = angleDegrees;

            var drive = body.xDrive;
            drive.target = angleDegrees;
            body.xDrive = drive;
        }

        /// <summary>
        /// 批量设置所有机械臂关节角度
        /// </summary>
        public void SetArmJointAngles(double[] angles)
        {
            for (int i = 0; i < angles.Length && i < 6; i++)
            {
                SetJointAngle($"joint{i + 1}", (float)angles[i]);
            }
        }

        /// <summary>
        /// 设置转台角度
        /// </summary>
        public void SetTurntableAngle(double angle)
        {
            SetJointAngle("plate_joint", (float)angle);
        }

        /// <summary>
        /// 获取当前关节角度（从 ArticulationBody 读取）
        /// </summary>
        public float GetJointAngle(string jointName)
        {
            if (_joints.TryGetValue(jointName, out var body))
                return (float)GetCurrentAngle(body);
            return 0f;
        }

        /// <summary>
        /// 获取所有机械臂关节角度
        /// </summary>
        public double[] GetArmJointAngles()
        {
            var angles = new double[6];
            for (int i = 0; i < 6; i++)
                angles[i] = GetJointAngle($"joint{i + 1}");
            return angles;
        }

        /// <summary>
        /// 获取转台角度
        /// </summary>
        public double GetTurntableAngle()
        {
            return GetJointAngle("plate_joint");
        }

        /// <summary>
        /// 从 ArticulationBody 获取当前角度
        /// </summary>
        private double GetCurrentAngle(ArticulationBody body)
        {
            // Revolute 关节的 jointPosition 第一分量即为角度（度）
            if (body.jointPosition.dofCount > 0)
                return body.jointPosition[0];
            return body.xDrive.target; // fallback
        }

        /// <summary>
        /// 从 ROS joint_states 更新关节（按名称后缀匹配，兼容 joint1 和 arm1_joint1）
        /// </summary>
        public void UpdateFromRosJointStates(string[] jointNames, double[] positions)
        {
            for (int i = 0; i < jointNames.Length && i < positions.Length; i++)
            {
                var rosName = jointNames[i];
                var angle = (float)positions[i];
                var matched = false;

                foreach (var key in _joints.Keys)
                {
                    if (rosName.EndsWith(key))
                    {
                        SetJointAngle(key, angle);
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                    Debug.LogWarning($"[PlatformJointController] 未匹配关节: {rosName}");
            }
        }

        /// <summary>
        /// 绑定 Ros2Bridge — 自动订阅 /joint_states 并驱动 7 关节
        /// </summary>
        public void Bind(FPT.Communication.Ros2Bridge ros2)
        {
            ros2.SubscribeJointStates((names, angles, vels, torques) =>
            {
                UpdateFromRosJointStates(names, angles);
            });
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器测试：将所有关节归零
        /// </summary>
        [ContextMenu("Zero All Joints")]
        public void ZeroAllJoints()
        {
            foreach (var name in _joints.Keys)
                SetJointAngle(name, 0f);
        }

        /// <summary>
        /// 编辑器测试：设置演示位姿
        /// </summary>
        [ContextMenu("Demo Pose")]
        public void DemoPose()
        {
            SetJointAngle("joint1", 30f);
            SetJointAngle("joint2", -45f);
            SetJointAngle("joint3", 60f);
            SetJointAngle("joint4", 20f);
            SetJointAngle("joint5", -10f);
            SetJointAngle("joint6", 0f);
            SetJointAngle("plate_joint", 90f);
        }
#endif
    }
}
