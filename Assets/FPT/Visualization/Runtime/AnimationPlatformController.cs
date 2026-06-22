using System.Collections.Generic;
using UnityEngine;

namespace FPT.Visualization
{
    public class AnimationPlatformController : MonoBehaviour
    {
        [SerializeField] private GameObject _platformRoot;

        private ArticulationBody _plateJoint;

        // _arms[1] = arm1 的 6 个 ArticulationBody, _arms[2] = arm2, _arms[3] = arm3
        private readonly Dictionary<int, ArticulationBody[]> _arms = new();

        public int ArmCount => _arms.Count;
        public bool IsReady => _plateJoint != null;

        private void Start()
        {
            if (_platformRoot == null)
                _platformRoot = gameObject;
            DiscoverJoints();
            Debug.Log($"[AnimationPlatformController] 发现转台 + {ArmCount} 个机械臂");
        }

        /// <summary>设置某个臂的 6 个关节角（度）[j1..j6]</summary>
        public void SetArmAngles(int armIndex, double[] angles)
        {
            if (!_arms.TryGetValue(armIndex, out var joints)) return;
            for (int i = 0; i < 6 && i < angles.Length && i < joints.Length; i++)
                if (joints[i] != null)
                    SetDrive(joints[i], (float)angles[i]);
        }

        /// <summary>设置转台角度</summary>
        public void SetPlateAngle(double angleDegrees)
        {
            if (_plateJoint != null)
                SetDrive(_plateJoint, (float)angleDegrees);
        }

        private void SetDrive(ArticulationBody body, float deg)
        {
            var d = body.xDrive; d.target = deg; body.xDrive = d;
        }

        private void DiscoverJoints()
        {
            foreach (var body in _platformRoot.GetComponentsInChildren<ArticulationBody>())
            {
                var name = body.gameObject.name;

                if (name == "platform_plate_Link")
                {
                    _plateJoint = body;
                }
                else
                {
                    // arm1_link3, arm2_link5, arm3_link1 ...
                    for (int arm = 1; arm <= 3; arm++)
                    {
                        var prefix = $"arm{arm}_link";
                        if (name.StartsWith(prefix) && int.TryParse(name.Replace(prefix, ""), out int idx) && idx >= 1 && idx <= 6)
                        {
                            if (!_arms.ContainsKey(arm))
                                _arms[arm] = new ArticulationBody[6];
                            _arms[arm][idx - 1] = body;
                            break;
                        }
                    }
                }
            }
        }

        // ── SendMessage 兼容：单参数方法 ──
        public void SetAllAngles(double[] angles) => SetArmAngles(1, angles);
        public void SetArm1Angles(double[] angles) => SetArmAngles(1, angles);
        public void SetArm2Angles(double[] angles) => SetArmAngles(2, angles);
        public void SetArm3Angles(double[] angles) => SetArmAngles(3, angles);

#if UNITY_EDITOR
        [ContextMenu("Zero All")]
        private void ZeroAll()
        {
            var zero = new double[6];
            for (int arm = 1; arm <= 3; arm++)
                SetArmAngles(arm, zero);
            SetPlateAngle(0);
        }
#endif
    }
}
