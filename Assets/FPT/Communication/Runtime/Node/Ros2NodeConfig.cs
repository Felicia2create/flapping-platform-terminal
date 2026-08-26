using UnityEngine;

namespace FPT.Communication
{
    /// <summary>
    /// ROS2 节点配置 — 通过 ScriptableObject 在 Inspector 中配置
    /// 使用 ROS# (ros-sharp) 连接 rosbridge_server
    /// </summary>
    [CreateAssetMenu(fileName = "Ros2NodeConfig", menuName = "FPT/ROS2 Node Config")]
    public class Ros2NodeConfig : ScriptableObject
    {
        [Header("节点信息")]
        [Tooltip("ROS2 节点名称")]
        public string NodeName = "unity_node";

        [Header("连接")]
        [Tooltip("rosbridge WebSocket 地址（如 ws://127.0.0.1:9090）")]
        public string RosbridgeUrl = "ws://127.0.0.1:9090";

        [Tooltip("启动时自动连接")]
        public bool AutoConnect = true;

        [Header("重连")]
        [Tooltip("重连间隔（秒）")]
        public float ReconnectInterval = 5f;

        [Tooltip("最大重连次数（-1 = 无限）")]
        public int MaxReconnectAttempts = -1;
    }
}
