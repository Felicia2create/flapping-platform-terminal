#if ROS2

using RosSharp.RosBridgeClient.MessageTypes.Sensor;

namespace RosSharp.RosBridgeClient.MessageTypes.FptInterface
{
    public class EEPlanResponse : Message
    {
        public const string RosMessageName = "fpt_interface/EEPlan";

        public JointState response { get; set; }

        public EEPlanResponse()
        {
            this.response = new JointState();
        }

        public EEPlanResponse(JointState response)
        {
            this.response = response;
        }
    }
}

#endif
