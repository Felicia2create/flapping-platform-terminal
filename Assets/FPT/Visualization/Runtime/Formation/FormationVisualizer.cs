using FPT.Business;
using UnityEngine;

namespace FPT.Visualization
{
    /// <summary>
    /// 动态阵型可视化器 — 根据当前动画模式切换连线拓扑。
    ///
    /// 三种连线模式：
    ///   - Breathing（呼吸聚散）: 闭合三角形（Arm1→Arm2→Arm3→Arm1）
    ///   - SequentialWave（波浪接力）: 开口折线（Arm1→Arm2→Arm3）
    ///   - Lissajous（8字轨迹）: 星型拓扑（Center→Arm1→Center→Arm2→Center→Arm3）
    ///
    /// 架构约束：
    ///   - 所有目标引用通过 Inspector 拖拽赋值，禁止使用 GameObject.Find
    ///   - AnimationDemoController 通过 Bind() 注入
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class FormationVisualizer : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // Inspector 配置
        // ═══════════════════════════════════════════

        [Header("目标引用（3 只仿真鸟或机械臂末端）")]
        [SerializeField] private Transform[] _targets = new Transform[3];

        [Header("连线外观")]
        [SerializeField] private Color _lineColor = new Color(0f, 0.7f, 1f, 0.6f); // 科技蓝，半透明
        [SerializeField] private float _lineWidth = 0.015f;
        [SerializeField] private bool _useEmission = true;
        [SerializeField] private Color _emissionColor = new Color(0f, 0.5f, 1f);
        [SerializeField] private float _emissionIntensity = 2f;

        [Header("高级")]
        [Tooltip("如果为 true，连线点会上移到目标的 Y + offset")]
        [SerializeField] private float _verticalOffset = 0f;

        [Header("星型拓扑设置")]
        [Tooltip("星型模式下中心点的垂直偏移")]
        [SerializeField] private float _centerYOffset = 0.05f;

        // ═══════════════════════════════════════════
        // 依赖注入
        // ═══════════════════════════════════════════

        private AnimationDemoController _demo;

        /// <summary>
        /// 由外部注入 AnimationDemoController 引用，使可视化器能感知当前模式。
        /// </summary>
        public void Bind(AnimationDemoController demo)
        {
            _demo = demo;
        }

        // ═══════════════════════════════════════════
        // 内部状态
        // ═══════════════════════════════════════════

        private LineRenderer _lineRenderer;
        private DemoFormationMode _lastMode = (DemoFormationMode)(-1); // 强制首次刷新

        // ═══════════════════════════════════════════
        // 生命周期
        // ═══════════════════════════════════════════

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            ConfigureLineRenderer();
        }

        private void LateUpdate()
        {
            // 如果模式发生变化，重新配置 LineRenderer
            if (_demo != null && _demo.CurrentMode != _lastMode)
            {
                _lastMode = _demo.CurrentMode;
                ReconfigureForMode(_lastMode);
            }

            UpdateVertices();
        }

        // ═══════════════════════════════════════════
        // 初始化
        // ═══════════════════════════════════════════

        private void ConfigureLineRenderer()
        {
            // 默认最大 6 个点（星型拓扑需要最多）
            _lineRenderer.positionCount = 6;
            _lineRenderer.loop = false;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.startWidth = _lineWidth;
            _lineRenderer.endWidth = _lineWidth;
            _lineRenderer.numCornerVertices = 4;
            _lineRenderer.numCapVertices = 4;

            // URP 兼容材质
            _lineRenderer.material = CreateEmissionMaterial();
            _lineRenderer.startColor = _lineColor;
            _lineRenderer.endColor = _lineColor;
        }

        // ═══════════════════════════════════════════
        // 模式切换
        // ═══════════════════════════════════════════

        private void ReconfigureForMode(DemoFormationMode mode)
        {
            switch (mode)
            {
                case DemoFormationMode.Breathing:
                    // 闭合三角形：4 个点（Arm1→Arm2→Arm3→Arm1）
                    _lineRenderer.positionCount = 4;
                    _lineRenderer.startColor = _lineColor;
                    _lineRenderer.endColor = _lineColor;
                    break;

                case DemoFormationMode.SequentialWave:
                    // 开口折线：3 个点（Arm1→Arm2→Arm3）
                    _lineRenderer.positionCount = 3;
                    _lineRenderer.startColor = _lineColor;
                    _lineRenderer.endColor = _lineColor;
                    break;

                case DemoFormationMode.Lissajous:
                    // 星型拓扑：6 个点（Center→Arm1→Center→Arm2→Center→Arm3）
                    _lineRenderer.positionCount = 6;
                    _lineRenderer.startColor = _lineColor;
                    _lineRenderer.endColor = _lineColor;
                    break;
            }
        }

        // ═══════════════════════════════════════════
        // 顶点更新
        // ═══════════════════════════════════════════

        private void UpdateVertices()
        {
            if (_targets == null || _targets.Length < 3) return;

            // 获取 3 个目标位置
            Vector3[] positions = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                if (_targets[i] == null) return;
                positions[i] = _targets[i].position;
                positions[i].y += _verticalOffset;
            }

            DemoFormationMode mode = _demo != null ? _demo.CurrentMode : DemoFormationMode.Breathing;

            switch (mode)
            {
                case DemoFormationMode.Breathing:
                    DrawClosedTriangle(positions);
                    break;

                case DemoFormationMode.SequentialWave:
                    DrawOpenPolyline(positions);
                    break;

                case DemoFormationMode.Lissajous:
                    DrawStarTopology(positions);
                    break;
            }
        }

        /// <summary>
        /// 呼吸聚散 — 闭合三角形：Arm1→Arm2→Arm3→Arm1
        /// </summary>
        private void DrawClosedTriangle(Vector3[] p)
        {
            _lineRenderer.SetPosition(0, p[0]);
            _lineRenderer.SetPosition(1, p[1]);
            _lineRenderer.SetPosition(2, p[2]);
            _lineRenderer.SetPosition(3, p[0]); // 闭合回第 1 个点
        }

        /// <summary>
        /// 波浪接力 — 开口折线：Arm1→Arm2→Arm3
        /// </summary>
        private void DrawOpenPolyline(Vector3[] p)
        {
            _lineRenderer.SetPosition(0, p[0]);
            _lineRenderer.SetPosition(1, p[1]);
            _lineRenderer.SetPosition(2, p[2]);
        }

        /// <summary>
        /// 8字轨迹 — 星型拓扑：Center→Arm1→Center→Arm2→Center→Arm3
        /// 像雷达锁定目标一样的放射状连线。
        /// </summary>
        private void DrawStarTopology(Vector3[] p)
        {
            // 计算 3 个臂的几何中心
            Vector3 center = (p[0] + p[1] + p[2]) / 3f;
            center.y += _centerYOffset;

            // 放射状连线：中心→Arm1→中心→Arm2→中心→Arm3
            _lineRenderer.SetPosition(0, center);
            _lineRenderer.SetPosition(1, p[0]);
            _lineRenderer.SetPosition(2, center);
            _lineRenderer.SetPosition(3, p[1]);
            _lineRenderer.SetPosition(4, center);
            _lineRenderer.SetPosition(5, p[2]);
        }

        // ═══════════════════════════════════════════
        // 材质创建
        // ═══════════════════════════════════════════

        /// <summary>
        /// 创建 URP 兼容的发光半透明材质。
        /// 优先使用 URP Particles/Unlit（支持 Emission + Alpha），
        /// 回退到 URP Unlit，最终回退到 Sprites/Default。
        /// </summary>
        private Material CreateEmissionMaterial()
        {
            Shader shader = null;

            // 优先：URP Particles/Unlit（同时支持 Emission 和透明）
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

            // 回退：URP Unlit
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");

            // 最终回退：Sprites/Default
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            var mat = new Material(shader);

            // 设置颜色和透明
            mat.SetColor("_BaseColor", _lineColor);
            mat.SetColor("_Color", _lineColor); // Particles/Unlit 兼容

            // 尝试设置 URP 透明模式
            if (mat.HasProperty("_SurfaceType"))
            {
                mat.SetFloat("_SurfaceType", 1f); // 1 = Transparent
                mat.SetFloat("_Blend", 0f);        // Alpha blend
                mat.SetFloat("_ZWrite", 0f);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            // 尝试设置 Blend Mode（内置管线兼容）
            if (mat.HasProperty("_Mode"))
            {
                mat.SetFloat("_Mode", 3f); // Transparent
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }

            // Emission
            if (_useEmission && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", _emissionColor * _emissionIntensity);
            }

            mat.name = "FormationLine_Emission";

            return mat;
        }
    }
}