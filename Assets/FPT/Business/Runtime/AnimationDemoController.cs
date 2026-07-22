using UnityEngine;
using FPT.Core;
using Newtonsoft.Json;
using System.Linq;

namespace FPT.Business
{
    /// <summary>
    /// 动画演示控制器（混合驱动版）
    ///
    ///   - Breathing / SequentialWave / Lissajous：实时数学演算
    ///   - VShape / YShape：离线 JSON 驱动（从 formation_trajectory.json 读取）
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

        [Header("JSON 轨迹（VShape / YShape 模式）")]
        [SerializeField] private TextAsset _trajectoryJson;

        // ═══════════════════════════════════════════
        //  内部数据
        // ═══════════════════════════════════════════

        [Header("特写镜头")]
        [SerializeField] private int _closeupTextureWidth = 512;
        [SerializeField] private int _closeupTextureHeight = 288;
        [SerializeField] private float _closeupDistance = 0.85f;
        [SerializeField] private float _closeupHeight = 0.42f;

        // ═══════════════════════════════════════════
        //  内部数据
        // ═══════════════════════════════════════════

        private AnimationTrajectoryData _trajectoryData;

        // ── JSON 播放器专用字段 ──
        private float _playbackTime;          // 播放时间轴（秒），只增不减
        private float _prevJsonTime;          // 上一帧有效时间，用于检测 Loop 折返
        private PlaybackLoopMode _loopMode = PlaybackLoopMode.PingPong;

        /// <summary>循环模式（Inspector / UI 可设）</summary>
        public PlaybackLoopMode LoopMode
        {
            get => _loopMode;
            set => _loopMode = value;
        }

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

        public RenderTexture CloseupTexture => _closeupTexture;

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
        private Renderer[] _animRenderers;
        private LineRenderer[] _animLineRenderers;
        private TrailRenderer[] _animTrailRenderers;
        private int _activePrototypeIndex = 0;
        private Camera _closeupCamera;
        private RenderTexture _closeupTexture;

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
            // 收集 AnimationPlatform 下所有渲染组件
            _animRenderers = _animPlatformRoot?.GetComponentsInChildren<Renderer>() ?? new Renderer[0];
            _animLineRenderers = _animPlatformRoot?.GetComponentsInChildren<LineRenderer>() ?? new LineRenderer[0];
            _animTrailRenderers = _animPlatformRoot?.GetComponentsInChildren<TrailRenderer>() ?? new TrailRenderer[0];
            CreateCloseupCamera();

            // 默认隐藏动画相关元素：控制面板模式下不可见
            SetRenderersEnabled(_animRenderers, false);
            SetLineRenderersEnabled(_animLineRenderers, false);
            SetTrailRenderersEnabled(_animTrailRenderers, false);

            // 解析离线 JSON 轨迹文件
            ParseTrajectoryJson();
        }

        private System.Collections.IEnumerator Start()
        {
            // 等待一帧，确保 AnimationPlatformController.Start() 已完成关节发现
            yield return null;
            ApplyInitialPose();
        }

        // ═══════════════════════════════════════════
        //  平台切换
        // ═══════════════════════════════════════════

        public void Activate()
        {
            SetRenderersEnabled(_realRenderers, false);
            SetRenderersEnabled(_animRenderers, true);
            SetLineRenderersEnabled(_animLineRenderers, true);
            SetTrailRenderersEnabled(_animTrailRenderers, true);
            ApplyPrototypeVisibility();
            _animPlatformRoot?.BroadcastMessage("SetTrailsEnabled", true, SendMessageOptions.DontRequireReceiver);
        }

        public void Deactivate()
        {
            PauseArm();
            SetRenderersEnabled(_realRenderers, true);
            SetRenderersEnabled(_animRenderers, false);
            SetLineRenderersEnabled(_animLineRenderers, false);
            SetTrailRenderersEnabled(_animTrailRenderers, false);
            _animPlatformRoot?.BroadcastMessage("SetTrailsEnabled", false, SendMessageOptions.DontRequireReceiver);
        }

        public void ShowPrototypeOnly(int prototypeIndex)
        {
            if (prototypeIndex < 1 || prototypeIndex > 3) return;

            _activePrototypeIndex = prototypeIndex;
            ApplyPrototypeVisibility();
            UpdateCloseupCamera();
        }

        public void ShowAllPrototypes()
        {
            _activePrototypeIndex = 0;
            ApplyPrototypeVisibility();
            UpdateCloseupCamera();
        }

        private void SetPrototypeVisible(int prototypeIndex, bool visible)
        {
            _animPlatformRoot?.BroadcastMessage(
                "SetArmVisible",
                new ArmVisibilityCommand(prototypeIndex, visible),
                SendMessageOptions.DontRequireReceiver
            );
        }

        private void ApplyPrototypeVisibility()
        {
            for (int prototypeIndex = 1; prototypeIndex <= 3; prototypeIndex++)
                SetPrototypeVisible(prototypeIndex, _activePrototypeIndex == 0 || prototypeIndex == _activePrototypeIndex);
        }

        private void CreateCloseupCamera()
        {
            if (_closeupTexture == null)
            {
                _closeupTexture = new RenderTexture(_closeupTextureWidth, _closeupTextureHeight, 16)
                {
                    name = "PrototypeCloseupTexture",
                    antiAliasing = 2
                };
            }

            if (_closeupCamera != null) return;

            var cameraObject = new GameObject("PrototypeCloseupCamera");
            cameraObject.transform.SetParent(transform, false);
            _closeupCamera = cameraObject.AddComponent<Camera>();
            _closeupCamera.targetTexture = _closeupTexture;
            _closeupCamera.clearFlags = CameraClearFlags.Skybox;
            _closeupCamera.fieldOfView = 24f;
            _closeupCamera.depth = -20f;
            _closeupCamera.enabled = true;

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                _closeupCamera.cullingMask = mainCamera.cullingMask;
                _closeupCamera.clearFlags = mainCamera.clearFlags;
                _closeupCamera.backgroundColor = mainCamera.backgroundColor;
            }

            UpdateCloseupCamera();
        }

        private void LateUpdate()
        {
            UpdateCloseupCamera();
        }

        private void UpdateCloseupCamera()
        {
            if (_closeupCamera == null || _animPlatformRoot == null) return;
            if (!TryGetCloseupBounds(out var bounds)) return;

            var focus = bounds.center;
            var size = Mathf.Max(bounds.extents.magnitude, 0.35f);
            var distance = Mathf.Max(_closeupDistance, size * 1.05f);
            var direction = new Vector3(0.75f, 0.45f, -1f).normalized;

            _closeupCamera.transform.position = focus - direction * distance + Vector3.up * _closeupHeight;
            _closeupCamera.transform.LookAt(focus + Vector3.up * Mathf.Min(size * 0.35f, 0.45f));
        }

        private bool TryGetCloseupBounds(out Bounds bounds)
        {
            bool hasBounds = false;
            bounds = new Bounds(_animPlatformRoot.transform.position, Vector3.one);

            foreach (var renderer in _animRenderers)
            {
                if (renderer == null || !ShouldIncludeInCloseup(renderer)) continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private bool ShouldIncludeInCloseup(Renderer renderer)
        {
            if (_activePrototypeIndex == 0) return true;
            return GetTransformPath(renderer.transform).Contains($"arm{_activePrototypeIndex}_");
        }

        private static string GetTransformPath(Transform transform)
        {
            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }

        private static void SetRenderersEnabled(MeshRenderer[] renderers, bool enabled)
        {
            foreach (var r in renderers)
                if (r != null) r.enabled = enabled;
        }

        private static void SetRenderersEnabled(Renderer[] renderers, bool enabled)
        {
            foreach (var r in renderers)
                if (r != null) r.enabled = enabled;
        }

        private static void SetLineRenderersEnabled(LineRenderer[] renderers, bool enabled)
        {
            foreach (var r in renderers)
                if (r != null) r.enabled = enabled;
        }

        private static void SetTrailRenderersEnabled(TrailRenderer[] renderers, bool enabled)
        {
            foreach (var r in renderers)
                if (r != null) r.enabled = enabled;
        }

        private void OnDestroy()
        {
            if (_closeupCamera != null)
                Destroy(_closeupCamera.gameObject);

            if (_closeupTexture != null)
            {
                _closeupTexture.Release();
                Destroy(_closeupTexture);
            }
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
            _playbackTime = 0;
            _prevJsonTime = 0;
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
            var modes = new[] {
                DemoFormationMode.SequentialWave,
                DemoFormationMode.Breathing,
                DemoFormationMode.Lissajous,
                DemoFormationMode.VShape,
                DemoFormationMode.YShape
            };
            if (index >= 0 && index < modes.Length)
                _currentMode = modes[index];
            StopArm();
        }

        /// <summary>
        /// 启动时将机械臂设置到轨迹初始姿态，避免显示默认静止姿态
        /// </summary>
        private void ApplyInitialPose()
        {
            if (_animPlatform == null) return;

            // ── JSON 驱动模式：从 JSON 读取初始姿态 ──
            if (_currentMode == DemoFormationMode.VShape ||
                _currentMode == DemoFormationMode.YShape)
            {
                if (_trajectoryData != null && _trajectoryData.PointCount > 0)
                {
                    var firstPoint = _trajectoryData.points[0];
                    for (int arm = 1; arm <= 3; arm++)
                    {
                        var armData = firstPoint.GetArm(arm);
                        if (armData?.positions_rad == null) continue;

                        double[] degAngles = armData.positions_rad
                            .Select(r => r * Mathf.Rad2Deg).ToArray();
                        _animPlatform.SendMessage($"SetArm{arm}Angles", degAngles);
                    }
                    Debug.Log("[AnimationDemo] JSON 初始姿态已应用");
                }
                return;
            }

            // ── 实时演算模式：从 TrajectoryGenerator 获取初始姿态 ──
            const int armCount = 3;
            for (int arm = 0; arm < armCount; arm++)
            {
                double[] radAngles = TrajectoryGenerator.GetJointAngles(
                    _currentMode, 0f, arm, _amplitude, _baseFrequency);
                
                double[] degAngles = new double[6];
                for (int j = 0; j < 6; j++)
                    degAngles[j] = radAngles[j] * Mathf.Rad2Deg;

                _animPlatform.SendMessage($"SetArm{arm + 1}Angles", degAngles);
            }

            Debug.Log($"[AnimationDemo] 启动初始化：{armCount} 臂已对齐轨迹初始姿态");
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

            // ── JSON 驱动模式：VShape / YShape ──
            if (_currentMode == DemoFormationMode.VShape ||
                _currentMode == DemoFormationMode.YShape)
            {
                _playbackTime += Time.deltaTime * _baseSpeed;
                DriveFromJson();
                return;
            }

            // ── 实时演算模式：Breathing / SequentialWave / Lissajous ──
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

        /// <summary>
        /// 从 TextAsset 解析 JSON 轨迹数据
        /// </summary>
        private void ParseTrajectoryJson()
        {
            if (_trajectoryJson == null || string.IsNullOrEmpty(_trajectoryJson.text))
            {
                Debug.LogWarning("[AnimationDemo] 轨迹 JSON 文件未设置，VShape/YShape 模式无法工作");
                _trajectoryData = null;
                return;
            }

            try
            {
                _trajectoryData = JsonConvert.DeserializeObject<AnimationTrajectoryData>(
                    _trajectoryJson.text
                );

                if (_trajectoryData?.points == null || _trajectoryData.PointCount == 0)
                {
                    Debug.LogWarning("[AnimationDemo] 解析 JSON 后没有找到轨迹点");
                    return;
                }

                Debug.Log(
                    "[AnimationDemo] JSON 轨迹加载成功: " +
                    $"schema={_trajectoryData.schema_version}, " +
                    $"formation={_trajectoryData.formation_type}, " +
                    $"points={_trajectoryData.PointCount}"
                );
            }
            catch (System.Exception e)
            {
                Debug.LogError("[AnimationDemo] 解析 JSON 轨迹失败: " + e.Message);
                _trajectoryData = null;
            }
        }

        /// <summary>
        /// JSON 驱动模式：基于 _playbackTime 在关键帧之间线性插值
        /// - Loop：到达末尾后回到开头（V→Y→V→Y...）
        /// - PingPong：到达末尾后反向播放（V→Y→V→Y...）
        /// </summary>
        private void DriveFromJson()
        {
            if (_trajectoryData == null || _trajectoryData.points == null || _trajectoryData.PointCount == 0)
                return;

            var points = _trajectoryData.points;
            int n = points.Length;
            float totalDuration = points[n - 1].t;
            if (totalDuration <= 0f) return;

            // ── 1. 根据循环模式计算有效时间 ──
            float effectiveT;
            if (_loopMode == PlaybackLoopMode.PingPong)
            {
                // PingPong：_playbackTime 永远递增，Mathf.PingPong 自动振荡 0→T→0→T...
                effectiveT = Mathf.PingPong(_playbackTime, totalDuration);
            }
            else
            {
                // Loop：取模折返 0→T→0→T...
                effectiveT = _playbackTime % totalDuration;
            }

            // ── 2. 找前后关键帧（支持 Loop 折返检测） ──
            int prevIdx = n - 1;
            int nextIdx = 0;

            // 检测 Loop 折返：上一帧接近末尾、当前帧接近开头 → 跨边界插值
            bool loopWrapped = _loopMode == PlaybackLoopMode.Loop
                && _prevJsonTime > totalDuration * 0.5f
                && effectiveT < totalDuration * 0.5f;

            if (loopWrapped)
            {
                // 折返段：从最后一个点插值到第一个点
                prevIdx = n - 1;
                nextIdx = 0;
            }
            else
            {
                // 正常段：找 effectiveT 落在哪个区间
                for (int i = 0; i < n; i++)
                {
                    if (points[i].t <= effectiveT)
                        prevIdx = i;
                    else
                    {
                        nextIdx = i;
                        break;
                    }
                }
                // 到达最后一个点之后：循环回第一个点
                if (prevIdx == n - 1 && effectiveT >= points[n - 1].t)
                    nextIdx = 0;
            }

            _prevJsonTime = effectiveT;

            var prev = points[prevIdx];
            var next = points[nextIdx];

            // ── 3. 计算插值比例 alpha ──
            float segStart = prev.t;
            float segEnd = nextIdx > prevIdx ? next.t : totalDuration;
            float segLen = segEnd - segStart;
            if (segLen <= 0f) segLen = 1f;
            float alpha = Mathf.Clamp01((effectiveT - segStart) / segLen);

            // ── 4. 对 3 个臂的 6 个关节做线性插值 ──
            double[] arm1Angles = null;
            for (int armIdx = 1; armIdx <= 3; armIdx++)
            {
                var prevArm = prev.GetArm(armIdx);
                var nextArm = next.GetArm(armIdx);
                if (prevArm == null || nextArm == null ||
                    prevArm.positions_rad == null || nextArm.positions_rad == null)
                    continue;

                double[] interp = new double[6];
                for (int j = 0; j < 6; j++)
                {
                    double a = prevArm.positions_rad[j];
                    double b = nextArm.positions_rad[j];
                    interp[j] = a + (b - a) * alpha;
                }

                double[] deg = interp.Select(r => r * Mathf.Rad2Deg).ToArray();
                _animPlatform.SendMessage($"SetArm{armIdx}Angles", deg);

                if (armIdx == 1) arm1Angles = deg;
            }

            // ── 5. UI 回调 ──
            if (arm1Angles != null)
                OnArmAnglesChanged?.Invoke(arm1Angles);

            ArmProgress = effectiveT / totalDuration;
            OnArmProgressChanged?.Invoke(ArmProgress);
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


    public readonly struct ArmVisibilityCommand
    {
        public readonly int ArmIndex;
        public readonly bool Visible;

        public ArmVisibilityCommand(int armIndex, bool visible)
        {
            ArmIndex = armIndex;
            Visible = visible;
        }
    }
}