namespace FPT.Core
{
    /// <summary>
    /// 机械臂运行模式
    /// </summary>
    public enum ArmOperationMode
    {
        /// <summary> 关节空间控制（各关节独立角度）</summary>
        JointSpace = 0,

        /// <summary> 笛卡尔空间控制（末端位姿 xyz + rpy）</summary>
        CartesianSpace = 1,

        /// <summary> 示教模式 </summary>
        Teaching = 2,

        /// <summary> 回零 </summary>
        Homing = 3,

        /// <summary> 空闲 </summary>
        Idle = 4,

        /// <summary> 错误/紧急停止 </summary>
        Error = 5,
    }
}
