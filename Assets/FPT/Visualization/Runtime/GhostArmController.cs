using System.Collections.Generic;
using FPT.Business;
using UnityEngine;

namespace FPT.Visualization
{
    /// <summary>
    /// 预览机械臂控制器 — 场景预置模式
    ///
    /// 【Editor 一次性设置】
    ///   1. 在 platform_plate_Link 下 Duplicate arm1_base_link → 改名 GhostArm
    ///   2. 删掉副本的全部 ArticulationBody、Urdf*、IsBaseLink 组件
    ///   3. 删掉副本的 Collisions 子节点
    ///   4. 挂上本脚本，Inspector 中 _sourceRoot 拖到 flapping_platform
    ///   5. （可选）MeshRenderer 换成半透明材质
    ///
    /// 【运行时】
    ///   自动在自己的子节点中找 arm1_link1~6 的 Transform，
    ///   从 sourceRoot 的真实 ArticulationBody 读取旋转轴，
    ///   订阅 InputTerminal.OnJointAnglesChanged 驱动 localRotation。
    ///   位置由父子层级自动跟随（与真实臂同为 platform_plate_Link 的子节点）。
    /// </summary>
    public class GhostArmController : MonoBehaviour
    {
        [Header("源模型根节点（用于读旋转轴）")]
        [SerializeField] private GameObject _sourceRoot;

        [Header("外观（如未在 Editor 中设材质则运行时创建）")]
        [SerializeField] private bool _applyMaterialAtRuntime = true;
        [SerializeField] private Color _ghostColor = new Color(0.3f, 0.6f, 1.0f, 0.4f);

        private static readonly string[] JointNames =
            { "joint1", "joint2", "joint3", "joint4", "joint5", "joint6" };

        private static readonly string[] LinkNames =
            { "arm1_link1", "arm1_link2", "arm1_link3", "arm1_link4", "arm1_link5", "arm1_link6" };

        // (Transform, 初始 localRotation, 局部旋转轴)
        private readonly Dictionary<string, (Transform t, Quaternion baseRot, Vector3 axis)> _joints = new();
        private Material _ghostMaterial;
        private InputTerminal _terminal;

        private void Start()
        {
            MapOwnChildJoints();
            if (_applyMaterialAtRuntime && _joints.Count > 0)
                ApplyGhostMaterial();
            Debug.Log($"[GhostArm] 场景预置模式, {_joints.Count} 个关节就绪");
        }

        public void Bind(InputTerminal terminal)
        {
            if (_terminal != null) _terminal.OnJointAnglesChanged -= OnJointsChanged;
            _terminal = terminal;
            if (_terminal != null) _terminal.OnJointAnglesChanged += OnJointsChanged;
        }

        // ═══════════════════════════════════════════
        // 在自己子层级找 arm1_link1~6，从 sourceRoot 读旋转轴
        // ═══════════════════════════════════════════

        private void MapOwnChildJoints()
        {
            _joints.Clear();
            for (int i = 0; i < 6; i++)
            {
                var t = FindDeepChild(transform, LinkNames[i]);
                if (t == null)
                {
                    Debug.LogError($"[GhostArm] 未找到子节点: {LinkNames[i]}");
                    _joints.Clear();
                    return;
                }
                Vector3 axis = ReadAxisFromSource(i);
                _joints[JointNames[i]] = (t, t.localRotation, axis);
            }
        }

        private Vector3 ReadAxisFromSource(int index)
        {
            if (_sourceRoot == null) return Vector3.right;
            var srcT = FindDeepChild(_sourceRoot.transform, LinkNames[index]);
            if (srcT == null) return Vector3.right;
            var ab = srcT.GetComponent<ArticulationBody>();
            if (ab != null && ab.jointType == ArticulationJointType.RevoluteJoint)
                return (ab.anchorRotation * Vector3.right).normalized;
            return Vector3.right;
        }

        // ═══════════════════════════════════════════
        // 材质
        // ═══════════════════════════════════════════

        private void ApplyGhostMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            _ghostMaterial = new Material(shader) { color = _ghostColor };
            _ghostMaterial.SetFloat("_Metallic", 0.1f);
            _ghostMaterial.SetFloat("_Smoothness", 0.5f);
            if (shader.name.Contains("URP") || shader.name.Contains("Universal"))
            { _ghostMaterial.SetFloat("_Surface", 1); _ghostMaterial.SetFloat("_Blend", 0); }
            else
            { _ghostMaterial.SetFloat("_Mode", 3); _ghostMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); _ghostMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); }
            _ghostMaterial.renderQueue = 3000;
            _ghostMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _ghostMaterial.EnableKeyword("_ALPHABLEND_ON");
            _ghostMaterial.SetOverrideTag("RenderType", "Transparent");

            foreach (var r in GetComponentsInChildren<MeshRenderer>())
                r.material = _ghostMaterial;
        }

        // ═══════════════════════════════════════════
        // 关节驱动
        // ═══════════════════════════════════════════

        private void OnJointsChanged(double[] angles)
        {
            for (int i = 0; i < JointNames.Length && i + 1 < angles.Length; i++)
            {
                if (!_joints.TryGetValue(JointNames[i], out var j)) continue;
                j.t.localRotation = j.baseRot * Quaternion.AngleAxis((float)angles[i + 1], j.axis);
            }
        }

        // ═══════════════════════════════════════════
        // 工具
        // ═══════════════════════════════════════════

        private static Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var f = FindDeepChild(child, name);
                if (f != null) return f;
            }
            return null;
        }

        private void OnDestroy()
        {
            Bind(null);
            if (_ghostMaterial != null) Destroy(_ghostMaterial);
        }
    }
}
