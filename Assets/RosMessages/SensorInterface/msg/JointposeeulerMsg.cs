#if ROS2

namespace RosSharp.RosBridgeClient.MessageTypes.SensorInterface
{
    /// <summary>
    /// 睿尔曼末端位姿（欧拉角 + 位置）
    /// euler: [roll, pitch, yaw] (rad)
    /// position: [x, y, z] (m)
    /// </summary>
    public class Jointposeeuler : Message
    {
        public const string RosMessageName = "rm_ros_interfaces/msg/Jointposeeuler";

        public float[] euler { get; set; }    // [roll, pitch, yaw]
        public float[] position { get; set; } // [x, y, z]

        public Jointposeeuler()
        {
            this.euler = new float[3];
            this.position = new float[3];
        }

        public Jointposeeuler(float[] euler, float[] position)
        {
            this.euler = euler ?? new float[3];
            this.position = position ?? new float[3];
        }
    }
}

#endif
