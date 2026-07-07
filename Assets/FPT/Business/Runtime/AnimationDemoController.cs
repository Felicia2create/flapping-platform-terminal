using UnityEngine;

namespace FPT.Business
{
    /// <summary>
    /// 动画演示控制器（参数化轨迹版）
    ///
    /// 废弃 JSON 驱动，改为实时数学演算。
    /// 每帧根据 DemoFormationMode 调用 TrajectoryGenerator 生成 3 臂关节角度。
    /// </summary>
    public class AnimationDemoController : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        //  Inspector 可调参数
        // ═══════════════════════════════════════════

        [Header("阵型模式")]
        [SerializeField] private DemoFormationMode _currentMode = DemoFormationMode.Breathing;

        [Header("运动参数")]
        [SerializeField, Range(0.1f, 5f)]
        private float _baseSpeed = 1.0f;

        [SerializeField, Range(0.01f, 1.5f)]
        private float _amplitude = 0.3f;  // 弧度

        [SerializeField, Range(0.1f, 3f)]
        private float _baseFrequency = 0.5f;  // Hz

        [Header("平台引用")]
        [SerializeField] private MonoBehaviour _animPlatform;
        [SerializeField] private GameObject _realPlatformRoot;
        [SerializeField] private GameObject _animPlatformRoot;

        [Header("转台")]
        [SerializeField] private float _plateSpeed = 0f;

        // ═══════════════════════════════════════════
        //  公共属性（供 Inspector / UI 使用）
        // ═══════════════════════════════════════════

        public DemoFormationMode CurrentMode
        {
            get => _currentMode;
            set => _currentMode = value;
        }

        public float BaseSpeed
        {
            get => _baseSpeed;
            set => _baseSpeed = Mathf.Clamp(value, 0.1f, 5f);
        }

        public float Amplitude
        {
            get => _amplitude;
            set => _amplitude = Mathf.Clamp(value, 0.01f, 1.5f);
        }

        public float BaseFrequency
        {
            get => _baseFrequency;
            set => _baseFrequency = Mathf.Clamp(value, 0.1f, 3f);
        }

        /// <summary>兼容旧接口：播放速度 = BaseSpeed</summary>
        public float PlaybackSpeed
        {
            get => _baseSpeed;
            set => _baseSpeed = Mathf.Clamp(value, 0.1f, 5f);
        }

        public float PlateSpeed
        {
            get => _plateSpeed;
            set => _plateSpeed = value;
        }

        // ═══════════════════════════════════════════
        //  状态
        // ═══════════════════════════════════════════

        public bool IsArmPlaying { get; private set; }
        public bool IsArmPaused { get; private set; }
        public float ArmProgress { get; private set; }

        // ═══════════════════════════════════════════
        //  事件（供 UI 订阅）
        // ═══════════════════════════════════════════

        public System.Action<float> OnArmProgressChanged;
        public System.Action<bool> OnArmPlayStateChanged;
        public System.Action<double[]> OnArmAnglesChanged;
        public System.Action<FlapFlightParams> OnFlightParamsChanged;

        // ═══════════════════════════════════════════
        //  内部状态
        // ═══════════════════════════════════════════

        private float _time;
        private float _plateAngle;
        private bool _plateActive;

        private MeshRenderer[] _realRenderers;
        private MeshRenderer[] _animRenderers;

        // ═══════════════════════════════════════════
        //  生命周期
        // ═══════════════════════════════════════════

        private void Awake()
        {
            // 自动查找平台
            if (_animPlatformRoot == null)
                _animPlatformRoot = GameObject.Find("AnimationPlatform") ?? GameObject.Find("AnimationPlatform ");
            if (_animPlatform == null && _animPlatformRoot != null)
                _animPlatform = _animPlatformRoot.GetComponent<MonoBehaviour>();
            if (_realPlatformRoot == null)
                _realPlatformRoot = GameObject.Find("flapping_platform");

            _realRenderers = _realPlatformRoot?.GetComponentsInChildren<MeshRenderer>() ?? new MeshRenderer[0];
            _animRenderers = _animPlatformRoot?.GetComponentsInChildren<MeshRenderer>() ?? new MeshRenderer[0];

            SetRenderersEnabled(_animRenderers, false);
        }

        // ═══════════════════════════════════════════
        //  平台切换
        // ═══════════════════════════════════════════

        public void Activate()
        {
            SetRenderersEnabled(_realRenderers, false);
            SetRenderersEnabled(_animRenderers, true);
        }

        public void Deactivate()
        {
            PauseArm();
            SetRenderersEnabled(_realRenderers, true);
            SetRenderersEnabled(_animRenderers, false);
        }

        private static void SetRenderersEnabled(MeshRenderer[] renderers, bool enabled)
        {
            foreach (var r in renderers)
                if (r != null) r.enabled = enabled;
        }

        // ═══════════════════════════════════════════
        //  播放控制
        // ═══════════════════════════════════════════

        public void PlayArm()
        {
            _plateActive = true;
            IsArmPlaying = true;
            IsArmPaused = false;
            OnArmPlayStateChanged?.Invoke(true);
        }

        public void PauseArm()
        {
            IsArmPlaying = false;
            IsArmPaused = true;
            _plateActive = false;
            OnArmPlayStateChanged?.Invoke(false);
        }

        public void StopArm()
        {
            IsArmPlaying = false;
            IsArmPaused = false;
            _plateActive = false;
            _time = 0;
            ArmProgress = 0;
            OnArmProgressChanged?.Invoke(0);
            OnArmPlayStateChanged?.Invoke(false);
        }

        /// <summary>
        /// 兼容旧 UI 路径选择接口（保留方法签名，内部切换模式）
        /// PathButton0 → SequentialWave, PathButton1 → Breathing, PathButton2 → Lissajous
        /// </summary>
        public void SelectPath(int index)
        {
            var modes = new[] { DemoFormationMode.SequentialWave, DemoFormationMode.Breathing, DemoFormationMode.Lissajous };
            if (index >= 0 && index < modes.Length)
                _currentMode = modes[index];
            StopArm();
        }

        // ═══════════════════════════════════════════
        //  每帧驱动
        // ═══════════════════════════════════════════

        private void Update()
        {
            if (_animPlatform == null) return;

            // 转台：独立旋转
            if (_plateActive && _plateSpeed > 0f)
            {
                _plateAngle += _plateSpeed * Time.deltaTime;
                _animPlatform.SendMessage("SetPlateAngle", _plateAngle);
            }

            // 机械臂：Play 后才动
            if (!IsArmPlaying) return;

            // 累加时间
            _time += Time.deltaTime * _baseSpeed;

            // 为 3 个臂分别生成关节角度
            double[] arm1Angles = null;
            for (int arm = 0; arm < 3; arm++)
            {
                // TrajectoryGenerator 输出单位：弧度
                double[] radAngles = TrajectoryGenerator.GetJointAngles(
                    _currentMode, _time, arm, _amplitude, _baseFrequency);

                // 转换为角度（AnimationPlatformController.SetArmAngles 需要角度）
                double[] degAngles = new double[6];
                for (int j = 0; j < 6; j++)
                    degAngles[j] = radAngles[j] * Mathf.Rad2Deg;

                _animPlatform.SendMessage($"SetArm{arm + 1}Angles", degAngles);

                if (arm == 0) arm1Angles = degAngles;
            }

            // 回传 arm1 角度给 UI 显示
            if (arm1Angles != null)
                OnArmAnglesChanged?.Invoke(arm1Angles);

            // 发射飞行参数
            EmitFlightParams();

            // 进度（归一化到 [0,1]，以 2π 为一个周期）
            float cyclePeriod = 1f / Mathf.Max(_baseFrequency, 0.01f);
            ArmProgress = Mathf.Repeat(_time, cyclePeriod) / cyclePeriod;
            OnArmProgressChanged?.Invoke(ArmProgress);
        }

        // ═══════════════════════════════════════════
        //  飞行参数估算
        // ═══════════════════════════════════════════

        private void EmitFlightParams()
        {
            if (OnFlightParamsChanged == null) return;

            float freq = _baseFrequency * _baseSpeed;
            float ampDeg = _amplitude * Mathf.Rad2Deg;
            float phase = 2f * Mathf.PI * _baseFrequency * _time;
            float speed = freq * ampDeg * 0.02f;          // 启发式空速 (m/s)
            float aoa = 8f + 6f * Mathf.Sin(phase);       // 攻角 (°)
            float lift = 1.8f * speed * speed;             // 升力 (N)
            float alt = 1.5f + 0.05f * Mathf.Sin(phase);  // 高度 (m)

            OnFlightParamsChanged.Invoke(new FlapFlightParams
            {
                FlapFrequencyHz = freq,
                FlapAmplitudeDeg = ampDeg,
                AirspeedMps = speed,
                AngleOfAttackDeg = aoa,
                LiftN = lift,
                AltitudeM = alt,
            });
        }
    }

    /// <summary> 扑翼无人机飞行参数快照 </summary>
    public struct FlapFlightParams
    {
        public float FlapFrequencyHz;
        public float FlapAmplitudeDeg;
        public float AirspeedMps;
        public float AngleOfAttackDeg;
        public float LiftN;
        public float AltitudeM;
    }
}