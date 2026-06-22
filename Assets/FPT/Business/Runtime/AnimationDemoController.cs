using FPT.Core;
using UnityEngine;

namespace FPT.Business
{
    public class AnimationDemoController : MonoBehaviour
    {
        [Header("轨迹 JSON（拖入 .json 文件）")]
        [SerializeField] private TextAsset _trajectoryJson;

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

        private void Awake()
        {
            if (_trajectoryJson != null)
                ParseJson();

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

        private void ParseJson()
        {
            _data = JsonUtility.FromJson<AnimationTrajectoryData>(_trajectoryJson.text);
            if (_data != null && _data.points != null && _data.points.Length > 0)
            {
                Debug.Log($"[AnimationDemo] JSON 解析: {_data.planning_group}, {_data.points.Length} 点, {_data.cycle_duration_sec:F1}s, {_data.sample_rate_hz}Hz");
            }
            else
            {
                Debug.LogWarning("[AnimationDemo] JSON 解析失败或轨迹为空");
            }
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
            for (int arm = 0; arm < _armCount; arm++)
            {
                _elapsedPerArm[arm] += Time.deltaTime * _playbackSpeed;
                if (_elapsedPerArm[arm] >= _data.cycle_duration_sec)
                    _elapsedPerArm[arm] -= _data.cycle_duration_sec;

                var angles = InterpolateArm(_elapsedPerArm[arm]);
                _animPlatform.SendMessage($"SetArm{arm + 1}Angles", angles);
            }

            OnArmProgressChanged?.Invoke(ArmProgress);
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
}
