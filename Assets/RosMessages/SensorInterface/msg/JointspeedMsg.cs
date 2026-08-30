#if ROS2

namespace RosSharp.RosBridgeClient.MessageTypes.SensorInterface
{
    /// <summary>
    /// 睿尔曼当前关节速度
    /// joint_speed: 各关节速度 (°/s)
    /// </summary>
    public class Jointspeed : Message
    {
        public const string RosMessageName = "rm_ros_interfaces/msg/Jointspeed";

        public float[] joint_speed { get; set; }

        public Jointspeed()
        {
            this.joint_speed = System.Array.Empty<float>();
        }

        public Jointspeed(float[] joint_speed)
        {
            this.joint_speed = joint_speed ?? System.Array.Empty<float>();
        }
    }
}

#endif
