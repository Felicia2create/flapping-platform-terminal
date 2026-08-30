using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FPT.UI
{
    /// <summary>
    /// 多曲线时间序列折线图 — 支持多条曲线叠加绘制在同一个绘图区域。
    /// X 轴为真实时间（滑动窗口），Y 轴自动量程（所有可见曲线共用）。
    /// 基于 Painter2D 绘制，使用固定容量环形缓冲。
    /// </summary>
    public class MultiLineTimeChart
    {
        // ── 单条曲线的数据层 ──
        public class Layer
        {
            public string ChannelId;
            public string DisplayName;
            public string Unit;
            public int Decimals;
            public Color Color;
            public Sample[] Buf;
            public int Count;
            public int Head;
            public bool Visible = true;
        }

        public struct Sample
        {
            public double T;
            public float V;
        }

        private readonly VisualElement _el;
        private readonly int _capacity;
        private readonly List<Layer> _layers = new List<Layer>();
        private readonly Color _gridColor;

        /// <summary> X 轴时间窗口（秒）</summary>
        public float WindowSeconds { get; set; } = 60f;

        /// <summary> 当前图层数 </summary>
        public int LayerCount => _layers.Count;

        /// <summary> 所有图层（只读）</summary>
        public IReadOnlyList<Layer> Layers => _layers;

        public MultiLineTimeChart(VisualElement el, int capacity)
        {
            _el = el;
            _capacity = Mathf.Max(2, capacity);
            _gridColor = new Color(0.12f, 0.47f, 0.90f, 0.16f);
            if (_el != null)
                _el.generateVisualContent += OnGenerate;
        }

        /// <summary> 添加一条曲线 </summary>
        public Layer AddLayer(string channelId, string displayName, string unit, int decimals, Color color)
        {
            // 去重
            var existing = GetLayer(channelId);
            if (existing != null) return existing;

            var layer = new Layer
            {
                ChannelId = channelId,
                DisplayName = displayName,
                Unit = unit,
                Decimals = decimals,
                Color = color,
                Buf = new Sample[_capacity],
                Count = 0,
                Head = 0
            };
            _layers.Add(layer);
            MarkDirty();
            return layer;
        }

        /// <summary> 移除一条曲线 </summary>
        public bool RemoveLayer(string channelId)
        {
            var idx = _layers.FindIndex(l => l.ChannelId == channelId);
            if (idx < 0) return false;
            _layers.RemoveAt(idx);
            MarkDirty();
            return true;
        }

        /// <summary> 获取指定图层 </summary>
        public Layer GetLayer(string channelId)
        {
            return _layers.Find(l => l.ChannelId == channelId);
        }

        /// <summary> 向指定通道追加一个样本 </summary>
        public void Push(string channelId, double t, float v)
        {
            var layer = GetLayer(channelId);
            if (layer == null)
            {
                Debug.LogWarning($"[Chart.Push] 图层 '{channelId}' 不存在！");
                return;
            }

            layer.Buf[layer.Head].T = t;
            layer.Buf[layer.Head].V = v;
            layer.Head = (layer.Head + 1) % layer.Buf.Length;
            if (layer.Count < layer.Buf.Length) layer.Count++;

            if (layer.Count == 1) // 第一个样本时打一次日志
                Debug.Log($"[Chart.Push] '{channelId}' 首个样本: v={v:F4}");

            _el?.MarkDirtyRepaint();
        }

        /// <summary> 清除所有图层数据 </summary>
        public void Clear()
        {
            foreach (var l in _layers)
            {
                l.Count = 0;
                l.Head = 0;
            }
            MarkDirty();
        }

        /// <summary> 清除指定图层数据 </summary>
        public void ClearLayer(string channelId)
        {
            var layer = GetLayer(channelId);
            if (layer == null) return;
            layer.Count = 0;
            layer.Head = 0;
            MarkDirty();
        }

        /// <summary> 请求重绘 </summary>
        public void Repaint() => _el?.MarkDirtyRepaint();


        /// <summary> 获取指定图层的最新值 </summary>
        public bool TryGetLatest(string channelId, out float v)
        {
            v = 0f;
            var layer = GetLayer(channelId);
            if (layer == null || layer.Count == 0) return false;
            v = layer.Buf[(layer.Head - 1 + layer.Buf.Length) % layer.Buf.Length].V;
            return true;
        }

        /// <summary> 获取指定图层在可见窗口内的值域 </summary>
        public bool TryGetVisibleRange(string channelId, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            var layer = GetLayer(channelId);
            if (layer == null || layer.Count == 0) { return false; }

            double tMax = LatestTime(layer);
            double tMin = tMax - WindowSeconds;
            bool any = false;

            for (int i = 0; i < layer.Count; i++)
            {
                var s = SampleAt(layer, i);
                if (s.T < tMin) continue;
                any = true;
                if (s.V < min) min = s.V;
                if (s.V > max) max = s.V;
            }
            if (!any) return false;
            if (max - min < 1e-4f) { min -= 1f; max += 1f; }
            return true;
        }

        /// <summary> 获取所有可见图层在窗口内的全局值域 </summary>
        public bool TryGetGlobalRange(out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            bool any = false;

            foreach (var layer in _layers)
            {
                if (!layer.Visible || layer.Count == 0) continue;
                double tMax = LatestTime(layer);
                double tMin = tMax - WindowSeconds;

                for (int i = 0; i < layer.Count; i++)
                {
                    var s = SampleAt(layer, i);
                    if (s.T < tMin) continue;
                    any = true;
                    if (s.V < min) min = s.V;
                    if (s.V > max) max = s.V;
                }
            }
            if (!any) return false;
            if (max - min < 1e-4f) { min -= 1f; max += 1f; }
            return true;
        }

        // ── 内部 ──

        private void MarkDirty()
        {
            if (_layers.Count > 0) _el?.MarkDirtyRepaint();
        }

        private double LatestTime(Layer layer)
            => layer.Count == 0 ? 0.0 : layer.Buf[(layer.Head - 1 + layer.Buf.Length) % layer.Buf.Length].T;

        private Sample SampleAt(Layer layer, int i)
        {
            int start = (layer.Head - layer.Count + layer.Buf.Length) % layer.Buf.Length;
            return layer.Buf[(start + i) % layer.Buf.Length];
        }

        private bool _loggedOnce;
        private void OnGenerate(MeshGenerationContext ctx)
        {
            if (_layers.Count == 0) return;

            var rect = _el.contentRect;
            if (!_loggedOnce)
            {
                _loggedOnce = true;
                int totalSamples = 0;
                foreach (var l in _layers) totalSamples += l.Count;
                Debug.Log($"[Chart.OnGenerate] layers={_layers.Count}, rect={rect.width:F0}x{rect.height:F0}, samples={totalSamples}");
            }
            if (rect.width <= 1f || rect.height <= 1f) return;

            const float padX = 4f;
            const float padY = 6f;
            float w = rect.width - padX * 2f;
            float h = rect.height - padY * 2f;

            var p = ctx.painter2D;

            // ── 网格 ──
            p.strokeColor = _gridColor;
            p.lineWidth = 1f;
            for (int i = 0; i < 3; i++)
            {
                float y = padY + h * i * 0.5f;
                p.BeginPath();
                p.MoveTo(new Vector2(padX, y));
                p.LineTo(new Vector2(padX + w, y));
                p.Stroke();
            }
            for (int i = 0; i < 3; i++)
            {
                float x = padX + w * i * 0.5f;
                p.BeginPath();
                p.MoveTo(new Vector2(x, padY));
                p.LineTo(new Vector2(x, padY + h));
                p.Stroke();
            }

            // ── 全局 Y 量程 ──
            if (!TryGetGlobalRange(out float gMin, out float gMax)) return;
            float span = gMax - gMin;
            double tMax = 0;
            bool hasTime = false;

            // 找到最新时间戳
            foreach (var layer in _layers)
            {
                if (!layer.Visible || layer.Count < 2) continue;
                double lt = LatestTime(layer);
                if (!hasTime || lt > tMax) { tMax = lt; hasTime = true; }
            }
            if (!hasTime) return;

            double tMin = tMax - WindowSeconds;

            // ── 逐层绘制 ──
            foreach (var layer in _layers)
            {
                if (!layer.Visible || layer.Count < 2) continue;

                p.strokeColor = layer.Color;
                p.lineWidth = 2f;
                p.lineJoin = LineJoin.Round;
                p.lineCap = LineCap.Round;
                p.BeginPath();

                bool started = false;
                for (int i = 0; i < layer.Count; i++)
                {
                    var s = SampleAt(layer, i);
                    if (s.T < tMin) { started = false; continue; }
                    float x = padX + (float)((s.T - tMin) / WindowSeconds) * w;
                    float y = padY + (1f - (s.V - gMin) / span) * h;
                    if (!started) { p.MoveTo(new Vector2(x, y)); started = true; }
                    else p.LineTo(new Vector2(x, y));
                }
                if (started) p.Stroke();
            }
        }
    }
}
