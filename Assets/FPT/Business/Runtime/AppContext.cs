using FPT.Communication;
using FPT.Core;
using UnityEngine;

namespace FPT.Business
{
    public class AppContext : MonoBehaviour
    {
        public static AppContext Instance { get; private set; }

        [Header("ROS2")]
        [SerializeField] private string _ros2Host = "127.0.0.1";
        [SerializeField] private int _ros2Port = 10000;
        [SerializeField] private bool _autoConnect = false;

        public Ros2Bridge Ros2Bridge { get; private set; }
        public Ros2PlanningBridge PlanningBridge { get; private set; }
        public InputTerminal InputTerminal { get; private set; }
        public CommunicationManager CommManager { get; private set; }
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

            // ROS2 桥接
            Ros2Bridge = new Ros2Bridge();
            PlanningBridge = new Ros2PlanningBridge(Ros2Bridge);
            InputTerminal = new InputTerminal(PlanningBridge);

            // 非ROS2设备管理器（传感器/变频器走串口）
            CommManager = new CommunicationManager();
            DeviceManager = new DeviceManager(CommManager);
            DeviceManager.OnAnyDeviceStateChanged += s =>
                Debug.Log($"[AppContext] {s.DeviceId} → {s.Connection}");

            // 机械臂驱动 — 通过 Ros2Bridge 通信
            ArmDriver = new RobotArmDriver("robot_arm");
            ArmDriver.Bind(Ros2Bridge);
            DeviceManager.RegisterDriver(ArmDriver);

            // 预留
            DeviceManager.RegisterDriver(new SensorDriver("sensors"));
            DeviceManager.RegisterDriver(new VfdMotorDriver("vfd_motor"));
            DeviceManager.RegisterDriver(new TurntableDriver("turntable"));

            Coordinator = new DeviceCoordinator(DeviceManager);
            Coordinator.Subscribe();

            Debug.Log($"[AppContext] {DeviceManager.Drivers.Count} 个设备已注册");

            // 动画演示控制器（由用户手动挂载，此处仅获取引用）
            AnimationDemo = GetComponent<AnimationDemoController>();

            if (_autoConnect) ConnectRos2();
        }

        public void ConnectRos2()
        {
            Ros2Bridge.Connect(_ros2Host, _ros2Port);
            Debug.Log($"[AppContext] ROS2 连接 {_ros2Host}:{_ros2Port}");
        }

        private void OnDestroy()
        {
            DeviceManager?.ShutdownAllAsync();
            if (Instance == this) Instance = null;
        }
    }
}
