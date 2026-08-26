#if ROS2

namespace RosSharp.RosBridgeClient.MessageTypes.FptInterface
{
    public class ExecuteResponse : Message
    {
        public const string RosMessageName = "fpt_interface/Execute";

        public bool success { get; set; }
        public string message { get; set; }

        public ExecuteResponse()
        {
            this.success = false;
            this.message = "";
        }

        public ExecuteResponse(bool success, string message)
        {
            this.success = success;
            this.message = message;
        }
    }
}

#endif
