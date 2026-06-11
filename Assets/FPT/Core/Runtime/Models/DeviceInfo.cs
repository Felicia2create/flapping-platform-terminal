namespace FPT.Core
{
    /// <summary>
    /// 设备基本信息
    /// </summary>
    public class DeviceInfo
    {
        /// <summary> 设备唯一标识（如 "robot_arm", "turntable"）</summary>
        public string DeviceId { get; set; }

        /// <summary> 设备显示名称 </summary>
        public string DisplayName { get; set; }

        /// <summary> 设备类型 </summary>
        public DeviceType Type { get; set; }

        /// <summary> 制造商 </summary>
        public string Manufacturer { get; set; }

        /// <summary> 型号 </summary>
        public string Model { get; set; }

        /// <summary> 序列号 </summary>
        public string SerialNumber { get; set; }

        /// <summary> 固件版本 </summary>
        public string FirmwareVersion { get; set; }
    }

    public enum DeviceType
    {
        Unknown = 0,
        RobotArm = 1,
        Turntable = 2,
        SensorModule = 3,
        VfdController = 4,
    }
}
