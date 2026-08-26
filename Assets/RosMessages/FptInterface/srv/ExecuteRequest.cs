#if ROS2

namespace RosSharp.RosBridgeClient.MessageTypes.FptInterface
{
    public class ExecuteRequest : Message
    {
        public const string RosMessageName = "fpt_interface/Execute";

        public bool execute { get; set; }

        public ExecuteRequest()
        {
            this.execute = false;
        }

        public ExecuteRequest(bool execute)
        {
            this.execute = execute;
        }
    }
}

#endif
