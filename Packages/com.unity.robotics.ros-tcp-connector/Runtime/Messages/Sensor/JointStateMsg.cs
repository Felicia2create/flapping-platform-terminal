namespace RosMessageTypes.Sensor
{
    public class JointStateMsg : Unity.Robotics.ROSTCPConnector.MessageGeneration.Message
    {
        public RosMessageTypes.Std.HeaderMsg header;
        public string[] name;
        public double[] position;
        public double[] velocity;
        public double[] effort;
    }
}