namespace RosMessageTypes.Geometry
{
    public class TransformStampedMsg : Unity.Robotics.ROSTCPConnector.MessageGeneration.Message
    {
        public RosMessageTypes.Std.HeaderMsg header;
        public string child_frame_id;
        public PoseMsg transform;
    }
}