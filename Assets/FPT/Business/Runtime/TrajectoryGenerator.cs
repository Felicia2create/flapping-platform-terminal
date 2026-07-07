using System;

namespace FPT.Business
{
    /// <summary>
    /// 阵型演示模式
    /// </summary>
    public enum DemoFormationMode
    {
        /// <summary>波浪接力 — 三臂依次产生正弦波浪</summary>
        SequentialWave,
        /// <summary>呼吸聚散 — 三臂同步向外展开、向内收缩</summary>
        Breathing,
        /// <summary>8字轨迹 — Lissajous 曲线，末端画 8 字</summary>
        Lissajous
    }

    /// <summary>
    /// 参数化轨迹生成引擎
    /// 纯逻辑类，根据数学公式在运行时实时生成 6 个关节角度（单位：度）。
    /// </summary>
    public static class TrajectoryGenerator
    {
        /// <summary>
        /// 核心方法：根据模式、时间、臂索引返回 6 个关节角度（度）
        /// </summary>
        /// <param name="mode">阵型模式</param>
        /// <param name="time">当前时间 t（由 BaseSpeed 控制流逝）</param>
        /// <param name="armIndex">臂索引：0=0°, 1=120°, 2=240°</param>
        /// <param name="amplitude">振幅（弧度）</param>
        /// <param name="baseFreq">基础频率（Hz）</param>
        /// <returns>6 个关节角度（度）</returns>
        public static double[] GetJointAngles(DemoFormationMode mode, float time, int armIndex, float amplitude, float baseFreq)
        {
            return mode switch
            {
                DemoFormationMode.SequentialWave => GenerateSequentialWave(time, armIndex, amplitude, baseFreq),
                DemoFormationMode.Breathing       => GenerateBreathing(time, armIndex, amplitude, baseFreq),
                DemoFormationMode.Lissajous       => GenerateLissajous(time, armIndex, amplitude, baseFreq),
                _ => new double[6]
            };
        }

        // ═══════════════════════════════════════════
        //  SequentialWave（波浪接力）
        //  三臂以 120° 相位差依次摆动，形成波浪传播
        // ═══════════════════════════════════════════

        private static double[] GenerateSequentialWave(float t, int armIndex, float A, float freq)
        {
            float omega = (float)(2.0 * Math.PI * freq);
            float phase = armIndex * (float)(2.0 * Math.PI / 3.0); // 0°, 120°, 240°
            float arg = omega * t - phase;

            return new double[]
            {
                A * Math.Sin(arg),                    // Joint 1 (Yaw)
                A * 0.5  * Math.Cos(arg),             // Joint 2 (Pitch)
                A * 0.3  * Math.Sin(arg + Math.PI / 4), // Joint 3
                A * 0.2  * Math.Cos(arg),             // Joint 4
                A * 0.15 * Math.Sin(arg),             // Joint 5
                0                                      // Joint 6（末端保持不动）
            };
        }

        // ═══════════════════════════════════════════
        //  Breathing（呼吸聚散）
        //  三臂完全同步，同时向外张开再收回
        // ═══════════════════════════════════════════

        private static double[] GenerateBreathing(float t, int armIndex, float A, float freq)
        {
            float omega = (float)(2.0 * Math.PI * freq);

            return new double[]
            {
                A * Math.Sin(omega * t),              // Joint 1 (Yaw)
                A * Math.Cos(omega * t),              // Joint 2 (Pitch)
                A * 0.4 * Math.Sin(omega * t),        // Joint 3
                A * 0.3 * Math.Cos(omega * t),        // Joint 4
                A * 0.2 * Math.Sin(omega * t),        // Joint 5
                0                                      // Joint 6
            };
        }

        // ═══════════════════════════════════════════
        //  Lissajous（8字轨迹）
        //  Joint1 频率 : Joint2 频率 = 1:2，末端画 8 字
        // ═══════════════════════════════════════════

        private static double[] GenerateLissajous(float t, int armIndex, float A, float freq)
        {
            float omega  = (float)(2.0 * Math.PI * freq);
            float phase  = armIndex * (float)(2.0 * Math.PI / 3.0);
            float arg1   = omega * t + phase;
            float arg2   = 2 * omega * t + phase;      // 2 倍频率

            return new double[]
            {
                A * Math.Sin(arg1),                    // Joint 1 (Yaw)   — 1x 频率
                A * Math.Sin(arg2),                    // Joint 2 (Pitch) — 2x 频率 → 8 字
                A * 0.3  * Math.Sin(arg1),             // Joint 3
                A * 0.2  * Math.Cos(arg2),             // Joint 4
                A * 0.15 * Math.Sin(arg1),             // Joint 5
                0                                      // Joint 6
            };
        }
    }
}