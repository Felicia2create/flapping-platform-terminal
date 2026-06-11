using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.BuiltinInterfaces;
using System.Collections.Generic;

public class ROSJointStateSubscriber : MonoBehaviour
{
    [Header("ROS Settings")]
    [SerializeField] private string rosTopicName = "/joint_states";

    [Header("Joint Mapping")]
    [SerializeField] private ArticulationBody[] robotJoints;
    
    [Header("ROS Joint Names Mapping")]
    [SerializeField] private string[] rosJointNames;  // 与robotJoints数组顺序对应，指定每个Unity关节对应的ROS关节名称
    
    // 这是一个辅助数组，用于记录每个关节的原始Drive，避免重复创建结构体
    private ArticulationDrive[] jointDrives;
    
    // 建立ROS关节名称到Unity关节索引的映射表
    private Dictionary<string, int> rosNameToIndex = new Dictionary<string, int>();
    
    void Start()
    {
        // 1. 验证并建立名称映射
        if (robotJoints == null || robotJoints.Length == 0)
        {
            Debug.LogError("Robot Joints array is not assigned in the Inspector!", this);
            return;
        }
        
        if (rosJointNames == null || rosJointNames.Length != robotJoints.Length)
        {
            Debug.LogError($"ROS Joint Names array length ({rosJointNames?.Length ?? 0}) does not match Robot Joints array length ({robotJoints.Length})! " +
                          "Please ensure both arrays have the same length and fill in the ROS joint names correctly.", this);
            return;
        }
        
        // 2. 初始化Drive数组和映射表
        jointDrives = new ArticulationDrive[robotJoints.Length];
        for (int i = 0; i < robotJoints.Length; i++)
        {
            if (robotJoints[i] != null)
            {
                // 存储当前关节的Drive配置
                jointDrives[i] = robotJoints[i].xDrive;
                
                // 建立ROS关节名称到Unity关节索引的映射
                string rosName = rosJointNames[i];
                if (!string.IsNullOrEmpty(rosName))
                {
                    if (rosNameToIndex.ContainsKey(rosName))
                    {
                        Debug.LogWarning($"Duplicate ROS joint name '{rosName}' detected! Only the first mapping will be used.", this);
                    }
                    else
                    {
                        rosNameToIndex[rosName] = i;
                        Debug.Log($"Mapped: ROS joint '{rosName}' -> Unity joint '{robotJoints[i].name}' (index {i})");
                    }
                }
                else
                {
                    Debug.LogWarning($"ROS joint name is empty for Unity joint '{robotJoints[i].name}' (index {i}). This joint will be skipped.", this);
                }
            }
            else
            {
                Debug.LogWarning($"Robot joint at index {i} is null!", this);
            }
        }
        
        // 3. 获取ROS连接的单例并订阅话题
        ROSConnection.GetOrCreateInstance().Subscribe<JointStateMsg>(rosTopicName, ReceiveJointStates);
        
        Debug.Log($"Subscribed to topic: {rosTopicName} with {rosNameToIndex.Count} mapped joints");
    }

    // 这是ROS消息的回调函数，当收到/joint_states消息时会被调用
    private void ReceiveJointStates(JointStateMsg jointState)
    {
        // 确保关节数组不为空
        if (robotJoints == null || jointDrives == null || rosNameToIndex == null) return;
        
        // 基于关节名称匹配，而不是顺序
        // 遍历ROS消息中的所有关节
        for (int i = 0; i < jointState.name.Length && i < jointState.position.Length; i++)
        {
            string rosJointName = jointState.name[i];
            float radianValue = (float)jointState.position[i];
            
            // 根据ROS关节名称查找对应的Unity关节索引
            if (rosNameToIndex.TryGetValue(rosJointName, out int unityIndex))
            {
                // 确保索引有效且关节存在
                if (unityIndex < robotJoints.Length && robotJoints[unityIndex] != null)
                {
                    // ROS中的关节位置单位是弧度(rad)，而Unity的ArticulationBody单位是度(deg)，需要转换
                    float targetPositionDeg = radianValue * Mathf.Rad2Deg;
                    
                    // 修改我们之前存储的Drive结构体的target值
                    jointDrives[unityIndex].target = targetPositionDeg;
                    
                    // 将修改后的Drive重新赋值给关节，关节就会开始运动到目标位置
                    robotJoints[unityIndex].xDrive = jointDrives[unityIndex];
                }
            }
            else
            {
                // 只警告一次未映射的关节，避免刷屏
                if (!warnedJoints.Contains(rosJointName))
                {
                    Debug.LogWarning($"Received ROS joint '{rosJointName}' which is not mapped to any Unity joint. " +
                                   "Please add it to the rosJointNames array if it should be controlled.", this);
                    warnedJoints.Add(rosJointName);
                }
            }
        }
    }
    
    // 用于记录已经警告过的未映射关节，避免重复警告
    private HashSet<string> warnedJoints = new HashSet<string>();
    
    // 可选：在编辑器中验证配置
    void OnValidate()
    {
        if (robotJoints != null && rosJointNames != null && robotJoints.Length != rosJointNames.Length)
        {
            Debug.LogWarning($"RobotJoints array length ({robotJoints.Length}) does not match RosJointNames array length ({rosJointNames.Length}). " +
                           "Please ensure they have the same length for proper mapping.", this);
        }
    }
}