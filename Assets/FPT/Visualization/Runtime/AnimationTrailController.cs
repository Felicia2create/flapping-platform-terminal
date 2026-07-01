using System.Collections.Generic;
using UnityEngine;

namespace FPT.Visualization
{
    /// <summary>
    /// 动画演示拖尾控制器 —— 在机械臂末端关节（如 arm1_link6）自动挂载 TrailRenderer。
    ///
    /// 使用方式：
    ///   将本脚本挂到场景中的 AnimationPlatform 对象上，
    ///   在 Inspector 中配置 _targetArms（如 {1} 表示给 arm1 加拖尾），
    ///   Play 后拖尾会跟随末端关节自动产生。
    ///
    /// 架构约束：
    ///   - 仅依赖 UnityEngine，不引用 FPT.Core / FPT.Communication / FPT.Business。
    ///   - 不修改任何 GameObject 的 Static 属性。
    ///   - 动态创建空子物体挂载 TrailRenderer，避免破坏 ArticulationBody 物理链。
    /// </summary>
    public class AnimationTrailController : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // Inspector 配置
        // ═══════════════════════════════════════════

        [Header("目标臂索引（1 = arm1，2 = arm2，3 = arm3）")]
        [SerializeField] private int[] _targetArms = { 1 };

        [Header("拖尾外观")]
        [SerializeField] private Color _trailColor = Color.cyan;
        [SerializeField] private float _trailWidth = 0.02f;
        [SerializeField] private float _trailTime = 10f;

        [Header("URP 材质（留空则自动创建）")]
        [SerializeField] private Material _trailMaterial;

        // ═══════════════════════════════════════════
        // 内部状态
        // ═══════════════════════════════════════════

        private readonly Dictionary<int, TrailRenderer> _trails = new();

        // ═══════════════════════════════════════════
        // 生命周期
        // ═══════════════════════════════════════════

        private void Start()
        {
            foreach (int armIdx in _targetArms)
            {
                Transform endEffector = FindEndEffector(armIdx);
                if (endEffector == null)
                {
                    Debug.LogWarning(
                        $"[AnimationTrailController] 未找到 arm{armIdx}_link6，跳过");
                    continue;
                }

                _trails[armIdx] = CreateTrail(endEffector);
                Debug.Log(
                    $"[AnimationTrailController] 已为 arm{armIdx}_link6 创建拖尾");
            }
        }

        // ═══════════════════════════════════════════
        // 公开方法
        // ═══════════════════════════════════════════

        /// <summary>
        /// 清除所有拖尾轨迹（急停 / 回零时调用，防止拉丝）。
        /// </summary>
        public void ClearAllTrails()
        {
            foreach (var kv in _trails)
            {
                if (kv.Value != null)
                    kv.Value.Clear();
            }
        }

        /// <summary>
        /// 启用 / 禁用全部拖尾渲染。
        /// </summary>
        public void SetTrailsEnabled(bool enabled)
        {
            foreach (var kv in _trails)
            {
                if (kv.Value != null)
                    kv.Value.enabled = enabled;
            }
        }

        // ═══════════════════════════════════════════
        // 内部实现
        // ═══════════════════════════════════════════

        /// <summary>
        /// 递归查找末端关节 GameObject（如 arm1_link6）。
        /// 不依赖外部 FindRecursive 扩展，自行实现以保持零外部依赖。
        /// </summary>
        private Transform FindEndEffector(int armIndex)
        {
            string targetName = $"arm{armIndex}_link6";
            return FindChildRecursive(transform, targetName);
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                var result = FindChildRecursive(parent.GetChild(i), name);
                if (result != null)
                    return result;
            }
            return null;
        }

        /// <summary>
        /// 在目标关节下动态创建空子物体并挂载 TrailRenderer。
        /// </summary>
        private TrailRenderer CreateTrail(Transform parent)
        {
            // 创建空子物体，避免向 ArticulationBody 节点添加额外组件
            var trailObj = new GameObject($"Trail_{parent.name}");
            trailObj.transform.SetParent(parent, false);
            trailObj.transform.localPosition = Vector3.zero;

            var trail = trailObj.AddComponent<TrailRenderer>();

            // ── 基本参数 ──
            trail.time = _trailTime;
            trail.startWidth = _trailWidth;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.005f;
            trail.autodestruct = false;

            // ── 对齐方式：修复 ArticulationBody 物理更新与渲染不同步导致的锯齿 ──
            trail.alignment = LineAlignment.TransformZ;

            // ── URP 兼容材质 ──
            trail.material = _trailMaterial ?? CreateDefaultUrpMaterial();

            // ── 渐变颜色：从不透明到透明 ──
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(_trailColor, 0f),
                    new GradientColorKey(_trailColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            trail.colorGradient = gradient;

            return trail;
        }

        /// <summary>
        /// 创建 URP 兼容的默认材质（Particles/Unlit），回退到 Sprites/Default。
        /// </summary>
        private static Material CreateDefaultUrpMaterial()
        {
            // 优先尝试 URP Particles/Unlit（项目使用 URP 渲染管线）
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                // 回退：Sprites/Default（内置管线，URP 也兼容）
                shader = Shader.Find("Sprites/Default");
            }
            if (shader == null)
            {
                // 最终回退
                shader = Shader.Find("Hidden/InternalErrorShader");
            }

            return new Material(shader);
        }
    }
}