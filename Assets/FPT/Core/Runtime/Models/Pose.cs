using UnityEngine;

namespace FPT.Core
{
    /// <summary>
    /// 表示三维空间中的位姿（位置 + 姿态）
    /// </summary>
    [System.Serializable]
    public struct DevicePose
    {
        /// <summary> 位置 x (m) </summary>
        public float X;

        /// <summary> 位置 y (m) </summary>
        public float Y;

        /// <summary> 位置 z (m) </summary>
        public float Z;

        /// <summary> 绕 X 轴旋转 Roll (度) </summary>
        public float Roll;

        /// <summary> 绕 Y 轴旋转 Pitch (度) </summary>
        public float Pitch;

        /// <summary> 绕 Z 轴旋转 Yaw (度) </summary>
        public float Yaw;

        public DevicePose(float x, float y, float z, float roll, float pitch, float yaw)
        {
            X = x;
            Y = y;
            Z = z;
            Roll = roll;
            Pitch = pitch;
            Yaw = yaw;
        }

        public Vector3 Position => new Vector3(X, Y, Z);
        public Vector3 RotationEuler => new Vector3(Roll, Pitch, Yaw);

        public override string ToString()
            => $"Pos({X:F3}, {Y:F3}, {Z:F3}) RPY({Roll:F1}, {Pitch:F1}, {Yaw:F1})";

        public static DevicePose Identity => new DevicePose(0, 0, 0, 0, 0, 0);

        /// <summary>
        /// 格式化单个位姿分量显示值：|value| &lt; threshold 时归零，最多保留 decimals 位小数
        /// 避免 ROS2 传回的极小值（如 0.4555e-14）以科学计数法显示
        /// </summary>
        /// <param name="value">原始浮点值</param>
        /// <param name="decimals">小数位数（位置 3，姿态 1）</param>
        /// <returns>格式化后的字符串，如 "0.000" 或 "1.234"</returns>
        public static string FormatComponent(float value, int decimals)
        {
            float threshold = Mathf.Pow(10f, -decimals) * 0.5f;
            float display = Mathf.Abs(value) < threshold ? 0f : value;
            return display.ToString($"F{decimals}");
        }

        /// <summary>
        /// 将低于显示阈值的极小值归零（直接用于 FloatField 等控件）
        /// </summary>
        /// <param name="value">原始浮点值</param>
        /// <param name="decimals">小数位数（位置 3，姿态 1）</param>
        /// <returns>归零或原值</returns>
        public static float ClampTinyValue(float value, int decimals)
        {
            float threshold = Mathf.Pow(10f, -decimals) * 0.5f;
            return Mathf.Abs(value) < threshold ? 0f : value;
        }
    }
}
