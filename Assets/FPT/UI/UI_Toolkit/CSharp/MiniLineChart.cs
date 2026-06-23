using UnityEngine;
using UnityEngine.UIElements;

namespace FPT.UI
{
    /// <summary>
    /// 简易折线图：固定容量环形缓冲，X 轴为时间（样本顺序，最旧→最新），Y 轴自动量程。
    /// 绑定到一个 VisualElement，通过 generateVisualContent 用 Painter2D 绘制。
    /// </summary>
    public class MiniLineChart
    {
        private readonly VisualElement _el;
        private readonly float[] _buf;
        private int _count;
        private int _head;
        private readonly Color _line;
        private readonly Color _grid;

        public MiniLineChart(VisualElement el, int capacity, Color line)
        {
            _el = el;
            _buf = new float[Mathf.Max(2, capacity)];
            _line = line;
            _grid = new Color(0.12f, 0.47f, 0.90f, 0.16f); // accent @ 低透明，作基线
            if (_el != null)
                _el.generateVisualContent += OnGenerate;
        }

        /// <summary> 追加一个时间样本 </summary>
        public void Push(float v)
        {
            _buf[_head] = v;
            _head = (_head + 1) % _buf.Length;
            if (_count < _buf.Length) _count++;
            _el?.MarkDirtyRepaint();
        }

        public void Clear()
        {
            _count = 0;
            _head = 0;
            _el?.MarkDirtyRepaint();
        }

        private float Sample(int i)
        {
            int start = (_head - _count + _buf.Length) % _buf.Length;
            return _buf[(start + i) % _buf.Length];
        }

        private void OnGenerate(MeshGenerationContext ctx)
        {
            var rect = _el.contentRect;
            if (rect.width <= 1f || rect.height <= 1f) return;

            const float padX = 4f;
            const float padY = 6f;
            float w = rect.width - padX * 2f;
            float h = rect.height - padY * 2f;

            var p = ctx.painter2D;

            // 基线（中线）
            p.strokeColor = _grid;
            p.lineWidth = 1f;
            p.BeginPath();
            p.MoveTo(new Vector2(padX, padY + h * 0.5f));
            p.LineTo(new Vector2(padX + w, padY + h * 0.5f));
            p.Stroke();

            if (_count < 2) return;

            // Y 自动量程
            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < _count; i++)
            {
                float v = Sample(i);
                if (v < min) min = v;
                if (v > max) max = v;
            }
            float span = max - min;
            if (span < 1e-4f) { min -= 1f; max += 1f; span = max - min; }

            p.strokeColor = _line;
            p.lineWidth = 2f;
            p.lineJoin = LineJoin.Round;
            p.lineCap = LineCap.Round;
            p.BeginPath();
            for (int i = 0; i < _count; i++)
            {
                float t = (float)i / (_count - 1);
                float v = Sample(i);
                float x = padX + t * w;
                float y = padY + (1f - (v - min) / span) * h;
                if (i == 0) p.MoveTo(new Vector2(x, y));
                else p.LineTo(new Vector2(x, y));
            }
            p.Stroke();
        }
    }
}
