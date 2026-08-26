#if ROS2

using RosSharp.RosBridgeClient.MessageTypes.Geometry;

namespace RosSharp.RosBridgeClient.MessageTypes.FptInterface
{
    public class EEPlanRequest : Message
    {
        public const string RosMessageName = "fpt_interface/EEPlan";

        public PoseStamped request { get; set; }

        public EEPlanRequest()
        {
            this.request = new PoseStamped();
        }

        public EEPlanRequest(PoseStamped request)
        {
            this.request = request;
        }
    }
}

#endif
