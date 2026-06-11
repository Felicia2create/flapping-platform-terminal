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
    }
}
