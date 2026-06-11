using UnityEngine;

namespace FPT.Visualization
{
    /// <summary>
    /// 参考网格和坐标轴渲染器 — 帮助在 3D 场景中定位
    /// </summary>
    public class GridRenderer : MonoBehaviour
    {
        [Header("网格设置")]
        [SerializeField] private float _gridSize = 5f;
        [SerializeField] private int _gridLines = 10;
        [SerializeField] private Color _gridColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private Color _centerLineColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);

        [Header("坐标轴")]
        [SerializeField] private float _axisLength = 1f;
        [SerializeField] private Color _xAxisColor = Color.red;
        [SerializeField] private Color _yAxisColor = Color.green;
        [SerializeField] private Color _zAxisColor = Color.blue;

        private LineRenderer _gridLineRenderer;

        private void Awake()
        {
            _gridLineRenderer = GetComponent<LineRenderer>();
            if (_gridLineRenderer == null)
                _gridLineRenderer = gameObject.AddComponent<LineRenderer>();

            _gridLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _gridLineRenderer.startWidth = 0.01f;
            _gridLineRenderer.endWidth = 0.01f;
            DrawGrid();
        }

        private void DrawGrid()
        {
            var points = new System.Collections.Generic.List<Vector3>();
            var half = _gridSize / 2f;
            var step = _gridSize / _gridLines;

            // 水平线（X 方向）
            for (int i = 0; i <= _gridLines; i++)
            {
                var z = -half + i * step;
                points.Add(new Vector3(-half, 0, z));
                points.Add(new Vector3(half, 0, z));
            }

            // 竖直线（Z 方向）
            for (int i = 0; i <= _gridLines; i++)
            {
                var x = -half + i * step;
                points.Add(new Vector3(x, 0, -half));
                points.Add(new Vector3(x, 0, half));
            }

            _gridLineRenderer.positionCount = points.Count;
            _gridLineRenderer.SetPositions(points.ToArray());
            _gridLineRenderer.startColor = _gridColor;
            _gridLineRenderer.endColor = _gridColor;
        }

        private void OnDrawGizmos()
        {
            // 坐标轴
            Gizmos.color = _xAxisColor;
            Gizmos.DrawLine(Vector3.zero, Vector3.right * _axisLength);
            Gizmos.color = _yAxisColor;
            Gizmos.DrawLine(Vector3.zero, Vector3.up * _axisLength);
            Gizmos.color = _zAxisColor;
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * _axisLength);
        }
    }
}
