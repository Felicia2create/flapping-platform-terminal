using System;
using System.Collections.Generic;
using System.Linq;
using FPT.Core;
using UnityEngine;

namespace FPT.UI
{
    /// <summary>
    /// 数据通道定义 — 描述一个可展示的数据源
    /// </summary>
    public class ChannelDefinition
    {
        public string Id;            // 唯一标识，如 "关节角度/joint1"
        public string DisplayName;   // 显示名，如 "joint1"
        public string Category;      // 分组，如 "关节角度"
        public string Unit;          // 单位，如 "°"
        public int Decimals;         // 小数位
        public Color Color;          // 曲线颜色
        public Func<double?> Read;   // 从当前状态读取最新值

        public override string ToString() => $"{DisplayName} ({Unit})";
    }

    /// <summary>
    /// 通道注册表 — 从 SensorState.Readings 动态生成通道。
    /// Readings 键格式："分类/名称"（如 "关节角度/joint1"、"力传感器/Fx"）。
    /// 新数据源出现时自动注册通道，无需硬编码。
    /// </summary>
    public class ChannelRegistry
    {
        private readonly List<ChannelDefinition> _all = new List<ChannelDefinition>();
        private readonly Dictionary<string, List<ChannelDefinition>> _byCategory
            = new Dictionary<string, List<ChannelDefinition>>();
        private readonly HashSet<string> _registeredIds = new HashSet<string>();

        // 分类颜色映射（按分类名分配颜色）
        private static readonly Color[] CategoryColors =
        {
            new Color(0.12f, 0.47f, 0.90f), // 蓝
            new Color(0.00f, 0.66f, 0.75f), // 青
            new Color(0.90f, 0.45f, 0.10f), // 橙
            new Color(0.55f, 0.35f, 0.80f), // 紫
            new Color(0.20f, 0.65f, 0.35f), // 绿
            new Color(0.90f, 0.25f, 0.30f), // 红
            new Color(0.80f, 0.60f, 0.10f), // 黄
            new Color(0.40f, 0.40f, 0.80f), // 靛
        };

        // 分类单位映射（已知分类的默认单位）
        private static readonly Dictionary<string, (string unit, int decimals)> KnownUnits
            = new Dictionary<string, (string, int)>
        {
            { "关节角度", ("°", 1) },
            { "关节速度", (" rad/s", 3) },
            { "关节力矩", (" Nm", 2) },
            { "末端位置", (" m", 3) },
            { "末端姿态", ("°", 1) },
            { "力传感器", (" N", 2) },
            { "IMU", ("", 4) },
        };

        private int _colorIndex;

        /// <summary> 所有已注册通道（只读）</summary>
        public IReadOnlyList<ChannelDefinition> All => _all;

        /// <summary> 所有分类名（按注册顺序）</summary>
        public IReadOnlyList<string> Categories => _byCategory.Keys.ToList();

        /// <summary> 注册一个通道（内部去重）</summary>
        public void Register(ChannelDefinition ch)
        {
            if (ch == null || string.IsNullOrEmpty(ch.Id)) return;
            if (_registeredIds.Contains(ch.Id)) return;

            _registeredIds.Add(ch.Id);
            _all.Add(ch);

            if (!_byCategory.TryGetValue(ch.Category, out var list))
            {
                list = new List<ChannelDefinition>();
                _byCategory[ch.Category] = list;
            }
            list.Add(ch);
        }

        /// <summary> 获取指定分类下的所有通道 </summary>
        public IReadOnlyList<ChannelDefinition> GetByCategory(string category)
        {
            return _byCategory.TryGetValue(category, out var list) ? list : Array.Empty<ChannelDefinition>();
        }

        /// <summary> 按 ID 查找通道 </summary>
        public ChannelDefinition Get(string id)
        {
            return _all.FirstOrDefault(c => c.Id == id);
        }

        /// <summary>
        /// 从 SensorState 动态注册通道。
        /// 扫描当前 Readings 的键，为未注册的键创建通道。
        /// DataPageController 在收到状态变更时调用此方法。
        /// </summary>
        public int SyncFromSensorState(SensorState state)
        {
            if (state?.Readings == null) return 0;

            int newCount = 0;
            foreach (var key in state.Readings.Keys)
            {
                if (_registeredIds.Contains(key)) continue;

                // 解析 "分类/名称" 格式
                int sep = key.IndexOf('/');
                string category = sep >= 0 ? key.Substring(0, sep) : "其他";
                string name = sep >= 0 ? key.Substring(sep + 1) : key;

                // 查找已知单位，或使用默认值
                var (unit, decimals) = KnownUnits.TryGetValue(category, out var known)
                    ? known
                    : ("", 2);

                // 分配颜色（每个分类一个颜色）
                var color = GetCategoryColor(category);

                Register(new ChannelDefinition
                {
                    Id = key,
                    DisplayName = name,
                    Category = category,
                    Unit = unit,
                    Decimals = decimals,
                    Color = color,
                    Read = () =>
                    {
                        var s = state;
                        return s?.Readings.TryGetValue(key, out var v) == true ? v : (double?)null;
                    }
                });

                newCount++;
            }

            return newCount;
        }

        private Color GetCategoryColor(string category)
        {
            // 为每个新分类分配一个颜色
            int idx = _byCategory.Count % CategoryColors.Length;
            if (_byCategory.TryGetValue(category, out var existing) && existing.Count > 0)
                return existing[0].Color;
            return CategoryColors[idx];
        }
    }
}
