using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FPT.UI
{
    /// <summary>
    /// 图表卡片 — 包含标题栏（含统计信息）、图例区、绘图区、X轴时间刻度。
    /// 支持拖放通道到此卡片以添加曲线，也支持移除已有曲线。
    /// </summary>
    public class ChartZone
    {
        // ── 配置 ──
        private const int ChartCapacity = 4096;

        // ── 核心 ──
        private readonly MultiLineTimeChart _chart;
        private readonly ChannelRegistry _registry;
        private int _zoneIndex;
        private int _colorIndex;

        // 七色：红橙黄绿青蓝紫
        private static readonly Color[] Rainbow =
        {
            new Color(1.00f, 0.25f, 0.25f), // 红
            new Color(1.00f, 0.55f, 0.15f), // 橙
            new Color(1.00f, 0.85f, 0.20f), // 黄
            new Color(0.30f, 0.85f, 0.40f), // 绿
            new Color(0.20f, 0.80f, 0.80f), // 青
            new Color(0.30f, 0.50f, 1.00f), // 蓝
            new Color(0.65f, 0.45f, 0.90f), // 紫
        };

        // ── UI 元素 ──
        private readonly VisualElement _root;
        private readonly VisualElement _plotArea;
        private readonly Label _titleLabel;
        private readonly Label _statsLabel;
        private readonly VisualElement _legendContainer;
        private readonly Label _emptyHint;
        private readonly Label _yMax, _yMid, _yMin;
        private readonly Label _xT0, _xT1;
        private readonly Button _closeButton;

        // ── 事件 ──
        /// <summary> 当用户点击关闭按钮时触发 </summary>
        public event Action<ChartZone> OnCloseRequested;

        /// <summary> 图表实例 </summary>
        public MultiLineTimeChart Chart => _chart;

        /// <summary> 根元素 </summary>
        public VisualElement Root => _root;

        /// <summary> 绘图区域（拖放目标）</summary>
        public VisualElement PlotArea => _plotArea;

        /// <summary> 此图表是否为空（无通道）</summary>
        public bool IsEmpty => _chart.LayerCount == 0;

        public ChartZone(VisualElement root, ChannelRegistry registry, int zoneIndex)
        {
            _root = root;
            _registry = registry;
            _zoneIndex = zoneIndex;

            // ── 查询 UI ──
            _titleLabel = root.Q<Label>("CzTitle");
            _statsLabel = root.Q<Label>("CzStats");
            _legendContainer = root.Q("CzLegend");
            _plotArea = root.Q("CzPlot");
            _emptyHint = root.Q<Label>("CzEmptyHint");
            _yMax = root.Q<Label>("CzYMax");
            _yMid = root.Q<Label>("CzYMid");
            _yMin = root.Q<Label>("CzYMin");
            _xT0 = root.Q<Label>("CzXT0");
            _xT1 = root.Q<Label>("CzXT1");
            _closeButton = root.Q<Button>("CzCloseBtn");

            // ── 创建图表 ──
            _chart = new MultiLineTimeChart(_plotArea, ChartCapacity);

            // ── 关闭按钮 ──
            if (_closeButton != null)
                _closeButton.clicked += () => OnCloseRequested?.Invoke(this);

            // ── 更新标题 ──
            UpdateTitle();
            UpdateEmptyHint();
        }

        /// <summary> 设置时间窗口 </summary>
        public void SetWindow(float seconds)
        {
            _chart.WindowSeconds = seconds;
            if (_xT0 != null) _xT0.text = $"-{seconds:F0}s";
            if (_xT1 != null) _xT1.text = $"-{seconds / 2f:F0}s";
        }

        /// <summary> 添加一条通道曲线（红橙黄绿青蓝紫循环）</summary>
        public bool AddChannel(ChannelDefinition ch)
        {
            if (ch == null) return false;
            var color = Rainbow[_colorIndex % Rainbow.Length];
            _colorIndex++;
            var layer = _chart.AddLayer(ch.Id, ch.DisplayName, ch.Unit, ch.Decimals, color);
            Debug.Log($"[ChartZone] 添加通道 '{ch.Id}'，当前图层数: {_chart.LayerCount}");
            UpdateTitle();
            UpdateLegend();
            UpdateEmptyHint();
            return true;
        }

        /// <summary> 移除一条通道曲线 </summary>
        public bool RemoveChannel(string channelId)
        {
            if (!_chart.RemoveLayer(channelId)) return false;
            UpdateTitle();
            UpdateLegend();
            UpdateEmptyHint();
            return true;
        }

        /// <summary> 清除此图表所有数据 </summary>
        public void Clear()
        {
            _chart.Clear();
        }

        /// <summary> 刷新统计标签（每帧调用）</summary>
        public void RefreshStats()
        {
            if (_chart.LayerCount == 0)
            {
                if (_statsLabel != null) _statsLabel.text = "暂无数据";
                return;
            }

            // 显示第一个图层的统计（或显示所有图层的最新值）
            var parts = new List<string>();
            foreach (var layer in _chart.Layers)
            {
                if (_chart.TryGetLatest(layer.ChannelId, out float latest))
                {
                    string fmt = $"F{layer.Decimals}";
                    parts.Add($"{layer.DisplayName}: {latest.ToString(fmt)}{layer.Unit}");
                }
            }
            if (_statsLabel != null)
                _statsLabel.text = parts.Count > 0 ? string.Join("  |  ", parts) : "暂无数据";

            // Y 轴刻度（使用全局量程）
            if (_chart.TryGetGlobalRange(out float gMin, out float gMax))
            {
                float mid = (gMin + gMax) * 0.5f;
                if (_yMax != null) _yMax.text = gMax.ToString("F1");
                if (_yMid != null) _yMid.text = mid.ToString("F1");
                if (_yMin != null) _yMin.text = gMin.ToString("F1");
            }
        }

        // ── 内部 ──

        private void UpdateTitle()
        {
            if (_titleLabel == null) return;
            if (_chart.LayerCount == 0)
                _titleLabel.text = $"图表 {_zoneIndex + 1}";
            else if (_chart.LayerCount == 1)
                _titleLabel.text = _chart.Layers[0].DisplayName;
            else
                _titleLabel.text = $"{_chart.Layers[0].DisplayName} +{_chart.LayerCount - 1}";
        }

        private void UpdateLegend()
        {
            if (_legendContainer == null) return;
            _legendContainer.Clear();

            foreach (var layer in _chart.Layers)
            {
                var item = new VisualElement();
                item.AddToClassList("cz-legend-item");

                var dot = new VisualElement();
                dot.AddToClassList("cz-legend-dot");
                dot.style.backgroundColor = layer.Color;
                item.Add(dot);

                var label = new Label(layer.DisplayName);
                label.AddToClassList("cz-legend-label");
                item.Add(label);

                // 移除按钮
                var removeBtn = new Button(() => RemoveChannel(layer.ChannelId));
                removeBtn.text = "×";
                removeBtn.AddToClassList("cz-legend-remove");
                item.Add(removeBtn);

                _legendContainer.Add(item);
            }
        }

        private void UpdateEmptyHint()
        {
            if (_emptyHint != null)
                _emptyHint.style.display = _chart.LayerCount == 0
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
        }
    }
}
