namespace RosMessageTypes.Geometry
{
    public class PoseStampedMsg : Unity.Robotics.ROSTCPConnector.MessageGeneration.Message
    {
        public RosMessageTypes.Std.HeaderMsg header;
        public PoseMsg pose;
    }
}