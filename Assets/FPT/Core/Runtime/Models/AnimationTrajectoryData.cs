using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace FPT.Core
{
    /// <summary>
    /// 动画轨迹数据 — 从离线 Python JSON 反序列化，支持多臂多阵型。
    /// 单位：positions_rad 弧度，target_world 米（Python 右手系）。
    /// </summary>
    [Serializable]
    public class AnimationTrajectoryData
    {
        [JsonProperty("schema_version")]
        public string schema_version;

        [JsonProperty("formation_type")]
        public string formation_type;

        [JsonProperty("description")]
        public string description;

        [JsonProperty("reference_frame")]
        public string reference_frame;

        [JsonProperty("joint_names")]
        public string[] joint_names;

        [JsonProperty("points")]
        public TrajectoryPoint[] points;

        public int PointCount => points?.Length ?? 0;
    }

    /// <summary>
    /// 单个臂的轨迹点数据
    /// </summary>
    [Serializable]
    public class ArmPointData
    {
        [JsonProperty("positions_rad")]
        public double[] positions_rad;

        [JsonProperty("target_world")]
        public double[] target_world;

        /// <summary>
        /// 将 Python 右手系 (X前, Y左, Z上) 转换为 Unity 左手系 (X右, Y上, Z前)
        /// 映射：Unity(x)=Python(x), Unity(y)=Python(z), Unity(z)=Python(y)
        /// </summary>
        public Vector3 TargetWorldUnity
        {
            get
            {
                if (target_world == null || target_world.Length < 3)
                    return Vector3.zero;
                return new Vector3(
                    (float)target_world[0],  // X → X
                    (float)target_world[2],  // Z → Y
                    (float)target_world[1]   // Y → Z
                );
            }
        }
    }

    /// <summary>
    /// 轨迹点 — 包含时间、段标签、三臂数据
    /// </summary>
    [Serializable]
    public class TrajectoryPoint
    {
        [JsonProperty("t")]
        public float t;

        [JsonProperty("segment")]
        public string segment;

        [JsonProperty("arms")]
        public Dictionary<string, ArmPointData> arms;

        /// <summary>
        /// 按臂索引获取数据（"1"→Arm1, "2"→Arm2, "3"→Arm3）
        /// </summary>
        public ArmPointData GetArm(int index)
        {
            if (arms == null) return null;
            arms.TryGetValue(index.ToString(), out var data);
            return data;
        }

        public Vector3 Arm1Target => GetArm(1)?.TargetWorldUnity ?? Vector3.zero;
        public Vector3 Arm2Target => GetArm(2)?.TargetWorldUnity ?? Vector3.zero;
        public Vector3 Arm3Target => GetArm(3)?.TargetWorldUnity ?? Vector3.zero;
    }
}