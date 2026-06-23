namespace RosMessageTypes.Trajectory
{
    public class JointTrajectoryPointMsg : Unity.Robotics.ROSTCPConnector.MessageGeneration.Message
    {
        public double[] positions;
        public double[] velocities;
        public double[] accelerations;
        public double[] effort;
        public double time_from_start;
    }
}