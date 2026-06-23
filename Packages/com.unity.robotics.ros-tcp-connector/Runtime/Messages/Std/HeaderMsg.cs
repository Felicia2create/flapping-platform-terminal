namespace RosMessageTypes.Std
{
    public class HeaderMsg : Unity.Robotics.ROSTCPConnector.MessageGeneration.Message
    {
        public uint seq;
        public string stamp;
        public string frame_id;
    }
}