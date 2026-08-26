using UnityEngine;
using UnityEngine.UIElements;

namespace FPT.UI
{
    /// <summary>
    /// 时间序列折线图：X 轴为真实时间（滑动窗口，最新在右），Y 轴自动量程。
    /// 固定容量环形缓冲，绑定到一个 VisualElement，通过 generateVisualContent 用 Painter2D 绘制。
    /// 多个实例可绑定到同一元素，用 Active 控制哪个实例真正绘制（通道切换）。
    /// </summary>
    public class TimeSeriesChart
    {
        private struct Sample
        {
            public double T;
            public float V;
        }

        private readonly VisualElement _el;
        private readonly Sample[] _buf;
        private int _count;
        private int _head;
        private readonly Color _line;
        private readonly Color _grid;

        /// <summary> 是否在此元素上绘制（多通道共用一个绘图元素时切换用）</summary>
        public bool Active { get; set; }

        /// <summary> X 轴时间窗口（秒）</summary>
        public float WindowSeconds { get; set; } = 60f;

        public TimeSeriesChart(VisualElement el, int capacity, Color line)
        {
            _el = el;
            _buf = new Sample[Mathf.Max(2, capacity)];
            _line = line;
            _grid = new Color(0.12f, 0.47f, 0.90f, 0.16f); // accent @ 低透明，作网格
            if (_el != null)
                _el.generateVisualContent += OnGenerate;
        }

        /// <summary> 追加一个带时间戳的样本（t 单位：秒，单调递增）</summary>
        public void Push(double t, float v)
        {
            _buf[_head].T = t;
            _buf[_head].V = v;
            _head = (_head + 1) % _buf.Length;
            if (_count < _buf.Length) _count++;
            if (Active) _el?.MarkDirtyRepaint();
        }

        public void Clear()
        {
            _count = 0;
            _head = 0;
            if (Active) _el?.MarkDirtyRepaint();
        }

        /// <summary> 请求重绘（切换 Active 通道后调用）</summary>
        public void Repaint() => _el?.MarkDirtyRepaint();

        public int Count => _count;

        /// <summary> 最新样本值；无数据返回 false </summary>
        public bool TryGetLatest(out float v)
        {
            if (_count == 0) { v = 0f; return false; }
            v = _buf[(_head - 1 + _buf.Length) % _buf.Length].V;
            return true;
        }

        /// <summary> 当前可见窗口内的值域；无可见样本返回 false </summary>
        public bool TryGetVisibleRange(out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            bool any = false;
            double tMin = LatestTime() - WindowSeconds;
            for (int i = 0; i < _count; i++)
            {
                var s = SampleAt(i);
                if (s.T < tMin) continue;
                any = true;
                if (s.V < min) min = s.V;
                if (s.V > max) max = s.V;
            }
            if (!any) return false;
            if (max - min < 1e-4f) { min -= 1f; max += 1f; }
            return true;
        }

        private double LatestTime()
            => _count == 0 ? 0.0 : _buf[(_head - 1 + _buf.Length) % _buf.Length].T;

        private Sample SampleAt(int i)
        {
            int start = (_head - _count + _buf.Length) % _buf.Length;
            return _buf[(start + i) % _buf.Length];
        }

        private void OnGenerate(MeshGenerationContext ctx)
        {
            if (!Active) return;

            var rect = _el.contentRect;
            if (rect.width <= 1f || rect.height <= 1f) return;

            const float padX = 4f;
            const float padY = 6f;
            float w = rect.width - padX * 2f;
            float h = rect.height - padY * 2f;

            var p = ctx.painter2D;

            // 网格：横向 3 条 + 纵向 3 条
            p.strokeColor = _grid;
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

            if (_count < 2) return;
            if (!TryGetVisibleRange(out float min, out float max)) return;

            float span = max - min;
            double tMax = LatestTime();
            double tMin = tMax - WindowSeconds;

            p.strokeColor = _line;
            p.lineWidth = 2f;
            p.lineJoin = LineJoin.Round;
            p.lineCap = LineCap.Round;
            p.BeginPath();
            bool started = false;
            for (int i = 0; i < _count; i++)
            {
                var s = SampleAt(i);
                if (s.T < tMin) { started = false; continue; }
                float x = padX + (float)((s.T - tMin) / WindowSeconds) * w;
                float y = padY + (1f - (s.V - min) / span) * h;
                if (!started) { p.MoveTo(new Vector2(x, y)); started = true; }
                else p.LineTo(new Vector2(x, y));
            }
            if (started) p.Stroke();
        }
    }
}
