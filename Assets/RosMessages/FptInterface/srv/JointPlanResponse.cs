#if ROS2

using RosSharp.RosBridgeClient.MessageTypes.Geometry;

namespace RosSharp.RosBridgeClient.MessageTypes.FptInterface
{
    public class JointPlanResponse : Message
    {
        public const string RosMessageName = "fpt_interface/JointPlan";

        public PoseStamped response { get; set; }

        public JointPlanResponse()
        {
            this.response = new PoseStamped();
        }

        public JointPlanResponse(PoseStamped response)
        {
            this.response = response;
        }
    }
}

#endif
