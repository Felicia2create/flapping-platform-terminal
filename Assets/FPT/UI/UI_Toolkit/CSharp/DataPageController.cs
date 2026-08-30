using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using FPT.Business;
using FPT.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace FPT.UI
{
    /// <summary>
    /// 数据分析页控制器 — 左侧通道标签可拖拽到右侧多个图表区域。
    /// 从 SensorDriver 读取所有数据，ChannelRegistry 动态注册通道。
    /// </summary>
    public class DataPageController : IDisposable
    {
        // ── 依赖 ──
        private readonly SensorDriver _sensorDriver;
        private readonly ChannelRegistry _registry;
        private readonly List<ChartZone> _charts = new List<ChartZone>();

        // ── 采样 ──
        private const double SampleInterval = 0.05; // 20 Hz
        private double _lastSampleTime = -1;
        private bool _paused;

        // ── 时间窗口 ──
        private static readonly float[] WindowOptions = { 10f, 30f, 60f, 120f };
        private static readonly string[] WindowLabels = { "10 秒", "30 秒", "60 秒", "120 秒" };
        private float _window = 60f;

        // ── UI 元素 ──
        private readonly VisualElement _root;
        private readonly ScrollView _channelScroll;
        private readonly ScrollView _chartArea;
        private readonly DropdownField _windowSelector;
        private readonly Button _pauseBtn;
        private readonly Button _clearBtn;
        private Button _addChartBtn;

        // ── 拖拽状态 ──
        private bool _isDragging;
        private ChannelDefinition _dragChannel;
        private IReadOnlyList<ChannelDefinition> _dragCategoryChannels;
        private VisualElement _dragItem;
        private VisualElement _dragPreview;
        private ChartZone _hoverChart;
        private const float DragThreshold = 8f;

        // ── 通道面板是否需要重建 ──
        private bool _channelPanelDirty = true;

        // ── 跨线程队列（ROS 回调可能在后台线程）──
        private readonly ConcurrentQueue<SensorState> _stateQueue = new ConcurrentQueue<SensorState>();

        public DataPageController(VisualElement root, SensorDriver sensorDriver)
        {
            _root = root;
            _sensorDriver = sensorDriver;

            // ── 初始化通道注册表 ──
            _registry = new ChannelRegistry();

            // ── 查询 UI ──
            _channelScroll = root.Q<ScrollView>("ChannelListScrollView");
            _chartArea = root.Q<ScrollView>("DataChartArea");
            _pauseBtn = root.Q<Button>("DaPauseButton");
            _clearBtn = root.Q<Button>("DaClearButton");

            // ── 滚动灵敏度 ──
            if (_channelScroll != null)
            {
                _channelScroll.mouseWheelScrollSize = 300f;
                _channelScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            }
            if (_chartArea != null)
            {
                _chartArea.mouseWheelScrollSize = 300f;
                _chartArea.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            }

            // ── 时间窗口下拉 ──
            var windowContainer = root.Q("DaWindowContainer");
            if (windowContainer != null)
            {
                _windowSelector = new DropdownField(new List<string>(WindowLabels), 2);
                _windowSelector.AddToClassList("speed-slider");
                _windowSelector.AddToClassList("fx-dropdown");
                _windowSelector.style.flexGrow = 1;
                windowContainer.Add(_windowSelector);
                _windowSelector.RegisterValueChangedCallback(_ => SetWindow(WindowOptions[_windowSelector.index]));
            }
            SetWindow(_window);

            // ── 按钮 ──
            if (_pauseBtn != null) _pauseBtn.clicked += OnPauseClicked;
            if (_clearBtn != null) _clearBtn.clicked += OnClearClicked;

            // ── 新建图表按钮 ──
            _addChartBtn = root.Q<Button>("AddChartButton");
            if (_addChartBtn != null)
                _addChartBtn.clicked += () => AddChartZone();

            // ── 创建默认图表 ──
            AddChartZone();

            // ── 订阅传感器状态 ──
            if (_sensorDriver != null)
                _sensorDriver.OnStateChanged += OnSensorStateChanged;
        }

        // ═══════════════════════════════════════════
        //  数据接收（来自 SensorDriver，可能在后台线程）
        // ═══════════════════════════════════════════

        private void OnSensorStateChanged(IDeviceState state)
        {
            // 只入队，不做任何 UI 操作（回调可能在后台线程）
            if (state is SensorState sensor)
                _stateQueue.Enqueue(sensor);
        }

        /// <summary>
        /// 每帧由主线程调用，处理队列中的状态更新。
        /// 所有 UI 操作（注册通道、Push 数据、刷新统计）都在主线程执行。
        /// </summary>
        public void Tick()
        {
            // 处理队列中的所有状态更新
            while (_stateQueue.TryDequeue(out var sensor))
            {
                // 动态注册新通道
                int newCount = _registry.SyncFromSensorState(sensor);
                if (newCount > 0)
                {
                    _channelPanelDirty = true;
                    Debug.Log($"[DataPage] 注册了 {newCount} 个新通道，总计 {_registry.All.Count} 个");
                }

                // 采样频率控制
                double t = Time.realtimeSinceStartupAsDouble;
                if (!_paused && t - _lastSampleTime >= SampleInterval)
                {
                    _lastSampleTime = t;

                    // 遍历所有图表的所有通道，Push 最新值
                    foreach (var chart in _charts)
                    {
                        foreach (var layer in chart.Chart.Layers)
                        {
                            var ch = _registry.Get(layer.ChannelId);
                            if (ch == null) continue;

                            var v = ch.Read();
                            if (v.HasValue)
                                chart.Chart.Push(layer.ChannelId, t, (float)v.Value);
                        }
                    }
                }
            }

            // 刷新统计（无论是否有新数据）
            foreach (var chart in _charts)
                chart.RefreshStats();

            // 延迟重建通道面板
            if (_channelPanelDirty)
            {
                _channelPanelDirty = false;
                BuildChannelPanel();
            }
        }

        // ═══════════════════════════════════════════
        //  通道面板构建
        // ═══════════════════════════════════════════

        private void BuildChannelPanel()
        {
            if (_channelScroll == null) return;
            _channelScroll.Clear();

            foreach (var category in _registry.Categories)
            {
                var channels = _registry.GetByCategory(category);
                var categoryEl = new VisualElement();
                categoryEl.AddToClassList("ch-category");

                // 分类标题（可折叠）
                var header = new VisualElement();
                header.AddToClassList("ch-category-header");

                var arrow = new Label("▶");
                arrow.AddToClassList("ch-category-arrow");
                header.Add(arrow);

                var title = new Label(category);
                title.AddToClassList("ch-category-title");
                header.Add(title);

                var count = new Label($"({channels.Count})");
                count.AddToClassList("ch-category-count");
                header.Add(count);

                categoryEl.Add(header);

                // 通道列表
                var list = new VisualElement();
                list.AddToClassList("ch-list");

                foreach (var ch in channels)
                {
                    var item = CreateChannelItem(ch);
                    list.Add(item);
                }

                categoryEl.Add(list);

                // 折叠交互（默认收起）+ 长按拖拽整组
                bool collapsed = true;
                list.style.display = DisplayStyle.None;
                bool catPointerActive = false;
                Vector3 catPointerStart = Vector3.zero;

                header.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button != 0) return;
                    catPointerActive = true;
                    catPointerStart = evt.position;
                    header.CapturePointer(evt.pointerId);
                    evt.StopPropagation();
                });
                header.RegisterCallback<PointerMoveEvent>(evt =>
                {
                    if (!catPointerActive) return;
                    if (!_isDragging && Vector3.Distance(evt.position, catPointerStart) > DragThreshold)
                    {
                        _isDragging = true;
                        _dragCategoryChannels = channels;
                        header.AddToClassList("dragging");

                        _dragPreview = new VisualElement();
                        _dragPreview.AddToClassList("drag-preview");
                        _dragPreview.Add(new Label($"{category} ({channels.Count})"));
                        _dragPreview.style.position = Position.Absolute;
                        _dragPreview.style.left = evt.position.x - _root.worldBound.x + 10;
                        _dragPreview.style.top = evt.position.y - _root.worldBound.y + 10;
                        _root.Add(_dragPreview);
                    }
                    if (_isDragging && _dragPreview != null)
                    {
                        _dragPreview.style.left = evt.position.x - _root.worldBound.x + 10;
                        _dragPreview.style.top = evt.position.y - _root.worldBound.y + 10;
                        UpdateHoverChart(evt.position);
                    }
                });
                header.RegisterCallback<PointerUpEvent>(evt =>
                {
                    if (!catPointerActive) return;
                    catPointerActive = false;
                    header.ReleasePointer(evt.pointerId);

                    if (_isDragging && _dragCategoryChannels != null && _dragCategoryChannels.Count > 0)
                    {
                        var target = _hoverChart;
                        var channelsToAdd = _dragCategoryChannels;
                        CleanupDrag();
                        if (target != null)
                        {
                            foreach (var ch in channelsToAdd)
                                target.AddChannel(ch);
                        }
                    }
                    else
                    {
                        CleanupDrag();
                        // 短按 → 折叠/展开
                        collapsed = !collapsed;
                        list.style.display = collapsed ? DisplayStyle.None : DisplayStyle.Flex;
                        arrow.text = collapsed ? "▶" : "▼";
                    }
                });

                _channelScroll.Add(categoryEl);
            }
        }

        private VisualElement CreateChannelItem(ChannelDefinition ch)
        {
            var item = new VisualElement();
            item.AddToClassList("ch-item");

            var label = new Label(ch.DisplayName);
            label.AddToClassList("ch-item-label");
            item.Add(label);

            var unit = new Label(ch.Unit);
            unit.AddToClassList("ch-item-unit");
            item.Add(unit);

            // ── 拖拽事件 ──
            item.RegisterCallback<PointerDownEvent>(evt => OnChannelPointerDown(evt, ch, item));
            item.RegisterCallback<PointerMoveEvent>(evt => OnChannelPointerMove(evt));
            item.RegisterCallback<PointerUpEvent>(evt => OnChannelPointerUp(evt));

            return item;
        }

        // ═══════════════════════════════════════════
        //  拖拽实现
        // ═══════════════════════════════════════════

        private void OnChannelPointerDown(PointerDownEvent evt, ChannelDefinition ch, VisualElement item)
        {
            _isDragging = true;
            _dragChannel = ch;
            _dragItem = item;
            item.AddToClassList("dragging");
            item.CapturePointer(evt.pointerId);
            Debug.Log($"[Drag] PointerDown '{ch.Id}' pos={evt.position} btn={evt.button} pointerId={evt.pointerId}");

            _dragPreview = new VisualElement();
            _dragPreview.AddToClassList("drag-preview");
            _dragPreview.Add(new Label(ch.DisplayName));
            _dragPreview.style.position = Position.Absolute;
            _dragPreview.style.left = evt.position.x - _root.worldBound.x + 10;
            _dragPreview.style.top = evt.position.y - _root.worldBound.y + 10;
            _root.Add(_dragPreview);

            evt.StopPropagation();
        }

        private void OnChannelPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging || _dragPreview == null) return;

            _dragPreview.style.left = evt.position.x - _root.worldBound.x + 10;
            _dragPreview.style.top = evt.position.y - _root.worldBound.y + 10;
            UpdateHoverChart(evt.position);
        }

        private void OnChannelPointerUp(PointerUpEvent evt)
        {
            if (!_isDragging) return;

            // 先保存目标，再清理（CleanupDrag 会置空 _hoverChart）
            var targetChart = _hoverChart;
            var targetChannel = _dragChannel;

            Debug.Log($"[Drag] PointerUp pos={evt.position} hoverChart={(targetChart != null ? "有" : "null")} channel='{targetChannel?.Id}'");

            if (_dragItem != null)
                _dragItem.ReleasePointer(evt.pointerId);

            CleanupDrag();

            if (targetChart != null && targetChannel != null)
            {
                Debug.Log($"[DataPage] 拖放通道 '{targetChannel.Id}' 到图表");
                targetChart.AddChannel(targetChannel);
            }
        }

        private void UpdateHoverChart(Vector3 pos)
        {
            ChartZone newHover = null;
            foreach (var chart in _charts)
            {
                if (chart.Root.worldBound.Contains(pos))
                {
                    newHover = chart;
                    break;
                }
            }
            if (newHover != _hoverChart)
            {
                if (_hoverChart != null)
                    _hoverChart.Root.RemoveFromClassList("drag-hover");
                _hoverChart = newHover;
                if (_hoverChart != null)
                    _hoverChart.Root.AddToClassList("drag-hover");
            }
        }

        private void CleanupDrag()
        {
            if (_dragPreview != null) { _root.Remove(_dragPreview); _dragPreview = null; }
            if (_dragItem != null) { _dragItem.RemoveFromClassList("dragging"); _dragItem = null; }
            if (_hoverChart != null) { _hoverChart.Root.RemoveFromClassList("drag-hover"); _hoverChart = null; }
            _isDragging = false;
            _dragChannel = null;
            _dragCategoryChannels = null;
        }

        // ═══════════════════════════════════════════
        //  图表管理
        // ═══════════════════════════════════════════

        private void AddChartZone()
        {
            if (_chartArea == null) return;

            var card = BuildChartCard();
            _chartArea.Add(card);

            var zone = new ChartZone(card, _registry, _charts.Count);
            zone.SetWindow(_window);
            zone.OnCloseRequested += RemoveChartZone;
            _charts.Add(zone);
        }

        private void RemoveChartZone(ChartZone zone)
        {
            _charts.Remove(zone);
            _chartArea?.Remove(zone.Root);
        }

        private VisualElement BuildChartCard()
        {
            var card = new VisualElement();
            card.AddToClassList("cz-card");

            var header = new VisualElement();
            header.AddToClassList("cz-header");
            var title = new Label("图表"); title.AddToClassList("cz-title"); title.name = "CzTitle";
            var stats = new Label("暂无数据"); stats.AddToClassList("cz-stats"); stats.name = "CzStats";
            var closeBtn = new Button { text = "×" }; closeBtn.AddToClassList("cz-close-btn"); closeBtn.name = "CzCloseBtn";
            header.Add(title); header.Add(stats); header.Add(closeBtn);
            card.Add(header);

            var legend = new VisualElement(); legend.AddToClassList("cz-legend"); legend.name = "CzLegend";
            card.Add(legend);

            var body = new VisualElement(); body.AddToClassList("cz-body");
            var yaxis = new VisualElement(); yaxis.AddToClassList("cz-yaxis");
            var yMax = new Label("--"); yMax.AddToClassList("cz-axis-label"); yMax.name = "CzYMax";
            var yMid = new Label("--"); yMid.AddToClassList("cz-axis-label"); yMid.name = "CzYMid";
            var yMin = new Label("--"); yMin.AddToClassList("cz-axis-label"); yMin.name = "CzYMin";
            yaxis.Add(yMax); yaxis.Add(yMid); yaxis.Add(yMin);
            body.Add(yaxis);
            var plot = new VisualElement(); plot.AddToClassList("cz-plot"); plot.name = "CzPlot";
            body.Add(plot);
            card.Add(body);

            var xaxis = new VisualElement(); xaxis.AddToClassList("cz-xaxis");
            var spacer = new Label(""); spacer.style.width = 40; xaxis.Add(spacer);
            var xT0 = new Label("-60s"); xT0.AddToClassList("cz-axis-label"); xT0.name = "CzXT0"; xaxis.Add(xT0);
            var xT1 = new Label("-30s"); xT1.AddToClassList("cz-axis-label"); xT1.name = "CzXT1"; xaxis.Add(xT1);
            var xNow = new Label("现在"); xNow.AddToClassList("cz-axis-label"); xaxis.Add(xNow);
            card.Add(xaxis);

            var hint = new Label("拖入通道开始 ↕"); hint.AddToClassList("cz-empty-hint"); hint.name = "CzEmptyHint";
            card.Add(hint);

            return card;
        }

        // ═══════════════════════════════════════════
        //  时间窗口 / 暂停 / 清除
        // ═══════════════════════════════════════════

        private void SetWindow(float seconds)
        {
            _window = seconds;
            foreach (var chart in _charts) chart.SetWindow(seconds);
        }

        private void OnPauseClicked()
        {
            _paused = !_paused;
            if (_pauseBtn != null) _pauseBtn.text = _paused ? "▶ 继续" : "⏸ 暂停全部";
        }

        private void OnClearClicked()
        {
            foreach (var chart in _charts) chart.Clear();
        }

        // ═══════════════════════════════════════════
        //  释放
        // ═══════════════════════════════════════════

        public void Dispose()
        {
            if (_sensorDriver != null)
                _sensorDriver.OnStateChanged -= OnSensorStateChanged;
            if (_pauseBtn != null) _pauseBtn.clicked -= OnPauseClicked;
            if (_clearBtn != null) _clearBtn.clicked -= OnClearClicked;
        }
    }
}
