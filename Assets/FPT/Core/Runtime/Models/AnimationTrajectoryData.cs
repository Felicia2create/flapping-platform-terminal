using System;
using UnityEngine;

namespace FPT.Core
{
    /// <summary>
    /// 动画轨迹数据 — 从 ROS2 JSON 反序列化（仅机械臂 6 关节，单位弧度）
    /// </summary>
    [Serializable]
    public class AnimationTrajectoryData
    {
        public string schema_version;
        public string planning_group;
        public string reference_frame;
        public string tool_link;
        public string[] joint_names;
        public float sample_rate_hz;
        public float cycle_duration_sec;

        public TrajectoryPoint[] points;

        public int PointCount => points?.Length ?? 0;
    }

    [Serializable]
    public class TrajectoryPoint
    {
        public float t;
        public string segment;
        public double[] positions_rad;
        public double[] velocities_rad_s;
        public double[] accelerations_rad_s2;
    }
}
