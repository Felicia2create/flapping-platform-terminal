using FPT.Core;
using UnityEngine;

namespace FPT.Business
{
    public class AnimationDemoController : MonoBehaviour
    {
        [Header("轨迹 JSON（拖入 .json 文件）")]
        [SerializeField] private TextAsset _trajectoryJson;

        [Header("多路径（最多 4 条，对应 UI 路径 1-4）")]
        [SerializeField] private TextAsset[] _trajectoryJsons = new TextAsset[4];

        [Header("平台")]
        [SerializeField] private MonoBehaviour _animPlatform;
        [SerializeField] private GameObject _realPlatformRoot;
        [SerializeField] private GameObject _animPlatformRoot;

        [Header("多臂")]
        [SerializeField] private int _armCount = 1;
        [SerializeField] private float _phaseOffset = 0f;

        [Header("设置")]
        [SerializeField] private float _playbackSpeed = 1.0f;

        private AnimationTrajectoryData _data;

        public bool IsArmPlaying { get; private set; }
        public bool IsArmPaused { get; private set; }
        public float ArmProgress { get; private set; }
        public float PlaybackSpeed
        {
            get => _playbackSpeed;
            set => _playbackSpeed = Mathf.Max(0.1f, value);
        }

        public float PlateSpeed { get; set; } = 0f;
        private bool _plateActive;
        private float _plateAngle;

        private float _elapsed;
        private float[] _elapsedPerArm;

        private MeshRenderer[] _realRenderers;
        private MeshRenderer[] _animRenderers;

        public AnimationTrajectoryData TrajectoryData => _data;

        public System.Action<float> OnArmProgressChanged;
        public System.Action<bool> OnArmPlayStateChanged;
        public System.Action<double[]> OnArmAnglesChanged;
        public System.Action<FlapFlightParams> OnFlightParamsChanged;

        private TextAsset[] _paths;
        private int _selectedPath;
        private float _flapAmplitudeDeg;

        public int SelectedPath => _selectedPath;

        private void Awake()
        {
            ResolvePaths();
            if (_paths[0] != null)
                ParsePath(0);

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

        private void ResolvePaths()
        {
            _paths = new TextAsset[4];
            // 1) 优先用 Inspector 配置的数组
            if (_trajectoryJsons != null)
                for (int i = 0; i < 4 && i < _trajectoryJsons.Length; i++)
                    _paths[i] = _trajectoryJsons[i];
            // 2) 槽 0 回退到单文件字段
            if (_paths[0] == null) _paths[0] = _trajectoryJson;
            // 3) 仍为空的槽尝试从 Resources/Trajectories 自动加载已知文件
            string[] known = { "unity_cycle", "up_down_demo" };
            for (int i = 0; i < known.Length; i++)
                if (_paths[i] == null)
                    _paths[i] = Resources.Load<TextAsset>($"Trajectories/{known[i]}");
        }

        /// <summary> 切换到第 index 条路径（0-3），由 UI 路径按钮调用 </summary>
        public void SelectPath(int index)
        {
            StopArm();
            ParsePath(index);
        }

        private void ParsePath(int index)
        {
            _selectedPath = Mathf.Clamp(index, 0, 3);
            var asset = _paths != null ? _paths[_selectedPath] : null;
            if (asset == null)
            {
                Debug.LogWarning($"[AnimationDemo] 路径 {_selectedPath + 1} 未配置轨迹 JSON");
                return;
            }

            _data = JsonUtility.FromJson<AnimationTrajectoryData>(asset.text);
            if (_data != null && _data.points != null && _data.points.Length > 0)
            {
                _flapAmplitudeDeg = ComputeFlapAmplitudeDeg(_data);
                Debug.Log($"[AnimationDemo] 路径{_selectedPath + 1} 解析: {_data.planning_group}, {_data.points.Length} 点, {_data.cycle_duration_sec:F1}s, {_data.sample_rate_hz}Hz");
            }
            else
            {
                Debug.LogWarning("[AnimationDemo] JSON 解析失败或轨迹为空");
            }
        }

        /// <summary> 取轨迹中各关节峰峰值的最大者，作为扑翼拍动幅度（度，半幅） </summary>
        private static float ComputeFlapAmplitudeDeg(AnimationTrajectoryData data)
        {
            float maxRange = 0f;
            for (int j = 0; j < 6; j++)
            {
                double min = double.MaxValue, max = double.MinValue;
                foreach (var p in data.points)
                {
                    if (p.positions_rad == null || j >= p.positions_rad.Length) continue;
                    if (p.positions_rad[j] < min) min = p.positions_rad[j];
                    if (p.positions_rad[j] > max) max = p.positions_rad[j];
                }
                if (max > min)
                {
                    float range = (float)(max - min) * Mathf.Rad2Deg;
                    if (range > maxRange) maxRange = range;
                }
            }
            return maxRange * 0.5f;
        }

        // ═══════════════════════════════════════════
        // 平台切换
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
        // 机械臂控制
        // ═══════════════════════════════════════════

        public void PlayArm()
        {
            _plateActive = true;
            if (_data == null || _data.points == null || _data.points.Length == 0)
            {
                Debug.LogWarning("[AnimationDemo] 无轨迹数据，仅转台旋转");
            }
            else
            {
                // 初始化每个臂的独立计时器 + 相位偏移
                _elapsedPerArm = new float[_armCount];
                float cycle = _data.cycle_duration_sec;
                float step = _phaseOffset > 0f ? _phaseOffset : (cycle / _armCount);
                for (int i = 0; i < _armCount; i++)
                    _elapsedPerArm[i] = step * i;

                IsArmPlaying = true;
                IsArmPaused = false;
            }
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
            _elapsed = 0;
            ArmProgress = 0;
            _elapsedPerArm = null;
            OnArmProgressChanged?.Invoke(0);
            OnArmPlayStateChanged?.Invoke(false);

            var wp0 = _data?.points?[0];
            if (wp0 != null && _animPlatform != null)
            {
                for (int arm = 1; arm <= _armCount; arm++)
                    _animPlatform.SendMessage($"SetArm{arm}Angles", GetAnglesFromPoint(wp0));
            }
        }

        private static double[] GetAnglesFromPoint(TrajectoryPoint pt)
        {
            var a = new double[6];
            for (int i = 0; i < 6 && i < pt.positions_rad.Length; i++)
                a[i] = pt.positions_rad[i] * Mathf.Rad2Deg;
            return a;
        }

        // ═══════════════════════════════════════════
        // 驱动
        // ═══════════════════════════════════════════

        private void Update()
        {
            if (_animPlatform == null) return;

            // 转台：独立旋转
            if (_plateActive && PlateSpeed > 0f)
            {
                _plateAngle += PlateSpeed * Time.deltaTime;
                _animPlatform.SendMessage("SetPlateAngle", _plateAngle);
            }

            // 机械臂：Play 后才动
            if (!IsArmPlaying || _data == null || _data.points == null) return;

            _elapsed += Time.deltaTime * _playbackSpeed;
            if (_elapsed >= _data.cycle_duration_sec)
                _elapsed -= _data.cycle_duration_sec;
            ArmProgress = Mathf.Clamp01(_elapsed / _data.cycle_duration_sec);

            // 驱动每个臂（相位偏移）
            double[] displayAngles = null;
            for (int arm = 0; arm < _armCount; arm++)
            {
                _elapsedPerArm[arm] += Time.deltaTime * _playbackSpeed;
                if (_elapsedPerArm[arm] >= _data.cycle_duration_sec)
                    _elapsedPerArm[arm] -= _data.cycle_duration_sec;

                var angles = InterpolateArm(_elapsedPerArm[arm]);
                if (arm == 0) displayAngles = angles;
                _animPlatform.SendMessage($"SetArm{arm + 1}Angles", angles);
            }

            if (displayAngles != null) OnArmAnglesChanged?.Invoke(displayAngles);
            EmitFlightParams();
            OnArmProgressChanged?.Invoke(ArmProgress);
        }

        /// <summary>
        /// 扑翼飞行参数：拍动频率/幅度取自轨迹，其余由频率·幅度·进度推导的简化估计。
        /// </summary>
        private void EmitFlightParams()
        {
            if (OnFlightParamsChanged == null) return;
            float cycle = _data?.cycle_duration_sec ?? 0f;
            float freq = cycle > 0.01f ? _playbackSpeed / cycle : 0f;
            float amp = _flapAmplitudeDeg;
            float phase = 2f * Mathf.PI * ArmProgress;
            float speed = freq * amp * 0.02f;          // 启发式空速 (m/s)
            float aoa = 8f + 6f * Mathf.Sin(phase);    // 攻角随拍动周期摆动 (°)
            float lift = 1.8f * speed * speed;         // 简化升力 ∝ v² (N)
            float alt = 1.5f + 0.05f * Mathf.Sin(phase); // 高度小幅起伏 (m)
            OnFlightParamsChanged.Invoke(new FlapFlightParams
            {
                FlapFrequencyHz = freq,
                FlapAmplitudeDeg = amp,
                AirspeedMps = speed,
                AngleOfAttackDeg = aoa,
                LiftN = lift,
                AltitudeM = alt,
            });
        }

        // ═══════════════════════════════════════════
        // 插值（弧度 JSON → 返回度数）
        // ═══════════════════════════════════════════

        private double[] InterpolateArm(float time)
        {
            var pts = _data.points;
            if (pts.Length == 0) return new double[6];

            if (time <= pts[0].t) return RadToDeg(pts[0].positions_rad);
            if (time >= pts[pts.Length - 1].t) return RadToDeg(pts[pts.Length - 1].positions_rad);

            for (int i = 0; i < pts.Length - 1; i++)
            {
                if (time >= pts[i].t && time <= pts[i + 1].t)
                {
                    float t = (time - pts[i].t) / (pts[i + 1].t - pts[i].t);
                    var r = new double[6];
                    for (int j = 0; j < 6; j++)
                        r[j] = (pts[i].positions_rad[j] + (pts[i + 1].positions_rad[j] - pts[i].positions_rad[j]) * t) * Mathf.Rad2Deg;
                    return r;
                }
            }
            return RadToDeg(pts[pts.Length - 1].positions_rad);
        }

        private static double[] RadToDeg(double[] rad)
        {
            var deg = new double[rad.Length];
            for (int i = 0; i < rad.Length; i++)
                deg[i] = rad[i] * Mathf.Rad2Deg;
            return deg;
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
