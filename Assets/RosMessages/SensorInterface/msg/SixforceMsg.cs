#if ROS2

namespace RosSharp.RosBridgeClient.MessageTypes.SensorInterface
{
    /// <summary>
    /// 睿尔曼六维力传感器数据（外受力）
    /// force_fx/fy/fz: 力 (N)
    /// force_mx/my/mz: 力矩 (N·m)
    /// </summary>
    public class Sixforce : Message
    {
        public const string RosMessageName = "rm_ros_interfaces/msg/Sixforce";

        public float force_fx { get; set; }
        public float force_fy { get; set; }
        public float force_fz { get; set; }
        public float force_mx { get; set; }
        public float force_my { get; set; }
        public float force_mz { get; set; }

        public Sixforce()
        {
            this.force_fx = 0f;
            this.force_fy = 0f;
            this.force_fz = 0f;
            this.force_mx = 0f;
            this.force_my = 0f;
            this.force_mz = 0f;
        }

        public Sixforce(float force_fx, float force_fy, float force_fz,
                         float force_mx, float force_my, float force_mz)
        {
            this.force_fx = force_fx;
            this.force_fy = force_fy;
            this.force_fz = force_fz;
            this.force_mx = force_mx;
            this.force_my = force_my;
            this.force_mz = force_mz;
        }
    }
}

#endif
