#if ROS2

namespace RosSharp.RosBridgeClient.MessageTypes.FptInterface
{
    public class PlanStatus : Message
    {
        public const string RosMessageName = "fpt_interface/msg/PlanStatus";

        public string status { get; set; }
        public string detail { get; set; }

        public PlanStatus()
        {
            this.status = "";
            this.detail = "";
        }

        public PlanStatus(string status, string detail)
        {
            this.status = status;
            this.detail = detail;
        }
    }
}

#endif
