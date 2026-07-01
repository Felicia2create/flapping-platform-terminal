using UnityEngine;

namespace FPT.Visualization
{
    /// <summary>
    /// 阵型可视化器 — 在 3 只仿真鸟之间绘制闭合三角形连线。
    ///
    /// 功能：
    ///   - 使用 LineRenderer 在 3 个目标之间绘制发光三角形
    ///   - 运行时自动创建 URP 兼容的 Emission 发光材质（科技蓝）
    ///   - 支持半透明效果
    ///
    /// 架构约束：
    ///   - 所有目标引用通过 Inspector 拖拽赋值，禁止使用 GameObject.Find
    ///   - 仅依赖 UnityEngine，不引用业务层
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

        // ═══════════════════════════════════════════
        // 内部状态
        // ═══════════════════════════════════════════

        private LineRenderer _lineRenderer;

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
            UpdateVertices();
        }

        // ═══════════════════════════════════════════
        // 初始化
        // ═══════════════════════════════════════════

        private void ConfigureLineRenderer()
        {
            // 闭合三角形：4 个点（首尾重合）
            _lineRenderer.positionCount = 4;
            _lineRenderer.loop = false; // 手动闭合，避免 LineRenderer 自动插值
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
        // 顶点更新
        // ═══════════════════════════════════════════

        private void UpdateVertices()
        {
            if (_targets == null || _targets.Length < 3) return;

            for (int i = 0; i < 3; i++)
            {
                if (_targets[i] == null) continue;

                Vector3 pos = _targets[i].position;
                pos.y += _verticalOffset;
                _lineRenderer.SetPosition(i, pos);
            }

            // 闭合：第 4 个点 = 第 1 个点
            if (_targets[0] != null)
            {
                Vector3 pos = _targets[0].position;
                pos.y += _verticalOffset;
                _lineRenderer.SetPosition(3, pos);
            }
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