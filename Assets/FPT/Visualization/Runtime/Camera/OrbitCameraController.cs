using UnityEngine;

namespace FPT.Visualization
{
    /// <summary>
    /// 轨道相机控制器 — 围绕目标旋转/缩放/平移
    /// 与 UI Toolkit 配合：通过 _activeArea 限制只在 CenterView 区域内响应输入
    /// </summary>
    public class OrbitCameraController : MonoBehaviour
    {
        [Header("目标")]
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _targetOffset = Vector3.zero;

        [Header("旋转")]
        [SerializeField] private float _rotateSpeed = 3f;
        [SerializeField] private float _minPitch = -80f;
        [SerializeField] private float _maxPitch = 80f;

        [Header("缩放")]
        [SerializeField] private float _zoomSpeed = 5f;
        [SerializeField] private float _minDistance = 0.5f;
        [SerializeField] private float _maxDistance = 20f;
        [SerializeField] private float _defaultDistance = 3f;

        [Header("平移")]
        [SerializeField] private float _panSpeed = 0.5f;

        [Header("平滑")]
        [SerializeField] private float _damping = 5f;

        // 当前状态
        private float _yaw;
        private float _pitch;
        private float _distance;
        private Vector3 _panOffset;

        // 目标值（平滑过渡用）
        private float _targetYaw;
        private float _targetPitch;
        private float _targetDistance;
        private Vector3 _targetPanOffset;

        private float _lastClickTime;
        private bool _isDragging;

        /// <summary>
        /// 相机操作的活跃区域（屏幕坐标，左下原点，对应 UI Toolkit Panel）
        /// 由 MainViewController 每帧更新。默认全屏。
        /// </summary>
        public Rect ActiveArea { get; set; } = new Rect(0, 0, Screen.width, Screen.height);

        private void Start()
        {
            if (_target != null)
            {
                var dir = transform.position - _target.position;
                _distance = dir.magnitude;
                _yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                _pitch = Mathf.Asin(dir.y / dir.magnitude) * Mathf.Rad2Deg;
            }
            else
            {
                _distance = transform.position.magnitude;
                _yaw = transform.eulerAngles.y;
                _pitch = -transform.eulerAngles.x;
            }
            _targetDistance = _distance;
            _targetYaw = _yaw;
            _targetPitch = _pitch;
        }

        private void LateUpdate()
        {
            HandleInput();
            SmoothValues();
            ApplyTransform();
        }

        /// <summary> 要排除的区域列表（如侧边面板），鼠标在此区域内时不响应相机操作 </summary>
        public System.Collections.Generic.List<Rect> ExcludeAreas { get; set; } = new System.Collections.Generic.List<Rect>();

        private bool IsMouseInActiveArea()
        {
            // Input.mousePosition 和 ToScreenRect 都是底部原点，不翻转
            var mp = (Vector2)Input.mousePosition;
            foreach (var r in ExcludeAreas)
                if (r.Contains(mp)) return false;
            return ActiveArea.Contains(mp);
        }

        private void HandleInput()
        {
            // 旋转：鼠标左键拖拽 — 仅在活跃区域内响应
            if (Input.GetMouseButtonDown(0) && IsMouseInActiveArea())
            {
                _isDragging = true;
                // 双击检测（聚焦）
                if (Time.unscaledTime - _lastClickTime < 0.3f)
                {
                    _targetPanOffset = Vector3.zero;
                    _targetDistance = _defaultDistance;
                }
                _lastClickTime = Time.unscaledTime;
            }

            if (Input.GetMouseButtonUp(0))
                _isDragging = false;

            if (_isDragging && IsMouseInActiveArea())
            {
                _targetYaw += Input.GetAxis("Mouse X") * _rotateSpeed;
                _targetPitch -= Input.GetAxis("Mouse Y") * _rotateSpeed;
                _targetPitch = Mathf.Clamp(_targetPitch, _minPitch, _maxPitch);
            }

            // 缩放：滚轮 — 仅在活跃区域内响应
            if (IsMouseInActiveArea())
            {
                var scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.001f)
                {
                    _targetDistance -= scroll * _zoomSpeed;
                    _targetDistance = Mathf.Clamp(_targetDistance, _minDistance, _maxDistance);
                }
            }

            // 平移：鼠标中键 — 仅在活跃区域内响应
            if (Input.GetMouseButton(2) && IsMouseInActiveArea())
            {
                var right = transform.right * -Input.GetAxis("Mouse X") * _panSpeed;
                var up = transform.up * -Input.GetAxis("Mouse Y") * _panSpeed;
                _targetPanOffset += right + up;
            }
        }

        private void SmoothValues()
        {
            var dt = Time.deltaTime * _damping;
            _yaw = Mathf.LerpAngle(_yaw, _targetYaw, dt);
            _pitch = Mathf.LerpAngle(_pitch, _targetPitch, dt);
            _distance = Mathf.Lerp(_distance, _targetDistance, dt);
            _panOffset = Vector3.Lerp(_panOffset, _targetPanOffset, dt);
        }

        private void ApplyTransform()
        {
            var focusPoint = _target != null
                ? _target.position + _targetOffset + _panOffset
                : _panOffset;

            var rotation = Quaternion.Euler(_pitch, _yaw, 0);
            var position = focusPoint - rotation * Vector3.forward * _distance;

            transform.position = position;
            transform.LookAt(focusPoint);
        }

        /// <summary> 设置注视目标 </summary>
        public void SetTarget(Transform target) { _target = target; }

        /// <summary> 重置视角 </summary>
        public void ResetView()
        {
            _targetYaw = 0;
            _targetPitch = 30;
            _targetDistance = _defaultDistance;
            _targetPanOffset = Vector3.zero;
        }
    }
}
