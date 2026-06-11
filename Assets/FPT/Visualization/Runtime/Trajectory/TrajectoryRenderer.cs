using System.Collections.Generic;
using FPT.Core;
using UnityEngine;

namespace FPT.Visualization
{
    /// <summary>
    /// 机械臂末端轨迹渲染器 — 用 LineRenderer 绘制末端运动路径
    /// </summary>
    public class TrajectoryRenderer : MonoBehaviour
    {
        [Header("组件")]
        [SerializeField] private LineRenderer _lineRenderer;

        [Header("设置")]
        [SerializeField] private int _maxPoints = 500;
        [SerializeField] private float _minDistanceBetweenPoints = 0.005f;
        [SerializeField] private Color _trajectoryColor = Color.cyan;
        [SerializeField] private float _lineWidth = 0.02f;

        private readonly List<Vector3> _points = new List<Vector3>();
        private Business.RobotArmDriver _armDriver;
        private Vector3 _lastRecordedPoint;

        private void Awake()
        {
            if (_lineRenderer == null)
                _lineRenderer = GetComponent<LineRenderer>();

            _lineRenderer.startWidth = _lineWidth;
            _lineRenderer.endWidth = _lineWidth;
            _lineRenderer.startColor = _trajectoryColor;
            _lineRenderer.endColor = _trajectoryColor;
            _lineRenderer.positionCount = 0;
        }

        /// <summary>
        /// 绑定机械臂驱动，开始追踪
        /// </summary>
        public void Bind(Business.RobotArmDriver armDriver)
        {
            _armDriver = armDriver;
            _armDriver.OnStateChanged += OnArmStateChanged;
        }

        private void OnArmStateChanged(IDeviceState state)
        {
            if (state is RobotArmState armState)
            {
                var pos = new Vector3(
                    armState.EndEffectorPose.X,
                    armState.EndEffectorPose.Y,
                    armState.EndEffectorPose.Z);

                if (_points.Count == 0 || Vector3.Distance(pos, _lastRecordedPoint) > _minDistanceBetweenPoints)
                {
                    AddPoint(pos);
                }
            }
        }

        private void AddPoint(Vector3 point)
        {
            _points.Add(point);
            _lastRecordedPoint = point;

            // 限制点数
            while (_points.Count > _maxPoints)
                _points.RemoveAt(0);

            _lineRenderer.positionCount = _points.Count;
            _lineRenderer.SetPositions(_points.ToArray());
        }

        /// <summary>
        /// 清除轨迹
        /// </summary>
        public void Clear()
        {
            _points.Clear();
            _lineRenderer.positionCount = 0;
        }

        /// <summary>
        /// 绘制预定义轨迹（如规划的路径）
        /// </summary>
        public void DrawPreviewPath(List<DevicePose> waypoints)
        {
            Clear();
            foreach (var wp in waypoints)
            {
                _points.Add(new Vector3(wp.X, wp.Y, wp.Z));
            }
            _lineRenderer.positionCount = _points.Count;
            _lineRenderer.SetPositions(_points.ToArray());
        }

        private void OnDestroy()
        {
            if (_armDriver != null)
                _armDriver.OnStateChanged -= OnArmStateChanged;
        }
    }
}
