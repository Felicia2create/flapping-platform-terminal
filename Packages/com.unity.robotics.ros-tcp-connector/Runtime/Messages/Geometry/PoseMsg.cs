namespace RosMessageTypes.Geometry
{
    public class PoseMsg : Unity.Robotics.ROSTCPConnector.MessageGeneration.Message
    {
        public PointMsg position;
        public QuaternionMsg orientation;
    }
}