using System;
using System.Collections.Generic;
using System.Linq;

namespace FPT.Core
{
    /// <summary>
    /// 统一传感器状态 — 所有可展示的数据汇入此状态。
    /// 键名格式："分类/名称"，如 "关节角度/joint1"、"力传感器/Fx"。
    /// ChannelRegistry 根据 Readings 的键动态生成可拖拽通道。
    /// </summary>
    public class SensorState : IDeviceState
    {
        public string DeviceId { get; set; }
        public DeviceConnectionState Connection { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public Dictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();

        // === 传感器数据 ===

        /// <summary>
        /// 所有传感器读数。
        /// 键 = "分类/名称"（如 "关节角度/joint1"、"力传感器/Fx"）
        /// 值 = 数值
        /// </summary>
        public Dictionary<string, double> Readings { get; set; } = new Dictionary<string, double>();

        /// <summary> 读数通道总数 </summary>
        public int ChannelCount => Readings.Count;

        /// <summary> 获取所有分类名（按 "/" 前缀分组）</summary>
        public IEnumerable<string> Categories =>
            Readings.Keys
                .Select(k => k.Contains('/') ? k.Substring(0, k.IndexOf('/')) : k)
                .Distinct();

        /// <summary> 获取指定分类下的所有读数（键为去掉前缀后的名称）</summary>
        public Dictionary<string, double> GetByCategory(string category)
        {
            var result = new Dictionary<string, double>();
            foreach (var kv in Readings)
            {
                int sep = kv.Key.IndexOf('/');
                if (sep >= 0 && kv.Key.Substring(0, sep) == category)
                    result[kv.Key.Substring(sep + 1)] = kv.Value;
                else if (sep < 0 && kv.Key == category)
                    result[kv.Key] = kv.Value;
            }
            return result;
        }

        /// <summary> 按键获取读数，不存在返回 null </summary>
        public double? GetValue(string key)
        {
            return Readings.TryGetValue(key, out var v) ? v : (double?)null;
        }
    }
}
