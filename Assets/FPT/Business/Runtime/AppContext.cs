using FPT.Communication;
using FPT.Core;
using UnityEngine;

namespace FPT.Business
{
    public class AppContext : MonoBehaviour
    {
        public static AppContext Instance { get; private set; }

        [Header("ROS2 节点")]
        [SerializeField] private Ros2NodeConfig _ros2NodeConfig;

        public Ros2Node Ros2Node { get; private set; }
        public InputTerminal InputTerminal { get; private set; }
        public DeviceManager DeviceManager { get; private set; }
        public DeviceCoordinator Coordinator { get; private set; }
        public RobotArmDriver ArmDriver { get; private set; }
        public AnimationDemoController AnimationDemo { get; private set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Init();
        }

        private void Init()
        {
            Debug.Log("[AppContext] 初始化...");

            // ROS2 节点（如果场景中没有 Ros2Node，自动创建）
            Ros2Node = Ros2Node.Instance;
            if (Ros2Node == null)
            {
                var nodeGo = new GameObject("Ros2Node");
                // 不设置父物体，让 Ros2Node 成为根 GameObject（DontDestroyOnLoad 要求）
                Ros2Node = nodeGo.AddComponent<Ros2Node>();
                // Ros2Node 会在 Awake 中自动初始化
            }

            // 数据同步中枢
            InputTerminal = new InputTerminal(Ros2Node);

            // 设备管理器
            DeviceManager = new DeviceManager();
            DeviceManager.OnAnyDeviceStateChanged += s =>
                Debug.Log($"[AppContext] {s.DeviceId} → {s.Connection}");

            // 机械臂驱动 — 通过 Ros2Node 通信
            ArmDriver = new RobotArmDriver("robot_arm");
            ArmDriver.Bind(Ros2Node);
            DeviceManager.RegisterDriver(ArmDriver);

            Coordinator = new DeviceCoordinator(DeviceManager);
            Coordinator.Subscribe();

            Debug.Log($"[AppContext] {DeviceManager.Drivers.Count} 个设备已注册");

            // 动画演示控制器（由用户手动挂载，此处仅获取引用）
            AnimationDemo = GetComponent<AnimationDemoController>();
        }

        private void OnDestroy()
        {
            DeviceManager?.ShutdownAllAsync();
            if (Instance == this) Instance = null;
        }
    }
}
