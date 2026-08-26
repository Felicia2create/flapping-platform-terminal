#if ROS2

using RosSharp.RosBridgeClient.MessageTypes.Sensor;

namespace RosSharp.RosBridgeClient.MessageTypes.FptInterface
{
    public class JointPlanRequest : Message
    {
        public const string RosMessageName = "fpt_interface/JointPlan";

        public JointState request { get; set; }

        public JointPlanRequest()
        {
            this.request = new JointState();
        }

        public JointPlanRequest(JointState request)
        {
            this.request = request;
        }
    }
}

#endif
