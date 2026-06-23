namespace RosMessageTypes.Trajectory
{
    public class JointTrajectoryMsg : Unity.Robotics.ROSTCPConnector.MessageGeneration.Message
    {
        public RosMessageTypes.Std.HeaderMsg header;
        public string[] joint_names;
        public JointTrajectoryPointMsg[] points;
    }
}