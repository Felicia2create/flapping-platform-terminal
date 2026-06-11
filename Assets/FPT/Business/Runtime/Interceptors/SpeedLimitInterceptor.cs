using System.Threading.Tasks;
using FPT.Core;

namespace FPT.Business
{
    /// <summary>
    /// 速度限制拦截器 — 检查指令速度是否在安全范围内
    /// </summary>
    public class SpeedLimitInterceptor : ICommandInterceptor
    {
        public string Name => "SpeedLimitCheck";

        /// <summary> 最大关节速度（度/秒）</summary>
        public double MaxJointSpeed { get; }

        /// <summary> 最大末端线速度（m/s）</summary>
        public double MaxEeSpeed { get; }

        public SpeedLimitInterceptor(double maxJointSpeed = 180, double maxEeSpeed = 2.0)
        {
            MaxJointSpeed = maxJointSpeed;
            MaxEeSpeed = maxEeSpeed;
        }

        public Task<CommandResult> Intercept(IDeviceCommand command, IDeviceState currentState)
        {
            if (command is JointCommand jointCmd)
            {
                if (jointCmd.TargetVelocities != null)
                {
                    foreach (var vel in jointCmd.TargetVelocities)
                    {
                        if (System.Math.Abs(vel) > MaxJointSpeed)
                            return Task.FromResult(
                                CommandResult.Fail(-1,
                                    $"关节速度 {vel}°/s 超过上限 {MaxJointSpeed}°/s"));
                    }
                }

                // 检查运动时间是否过短（隐含速度过高）
                if (jointCmd.Duration > 0 && currentState is RobotArmState armState)
                {
                    // 粗略估算：最大关节位移 / 时间
                    for (int i = 0; i < jointCmd.TargetAngles.Length; i++)
                    {
                        if (i < armState.JointAngles.Length)
                        {
                            var delta = System.Math.Abs(
                                jointCmd.TargetAngles[i] - armState.JointAngles[i]);
                            var speed = delta / jointCmd.Duration;
                            if (speed > MaxJointSpeed)
                                return Task.FromResult(
                                    CommandResult.Fail(-1,
                                        $"关节 J{i + 1} 估算速度 {speed:F1}°/s 超过上限"));
                        }
                    }
                }
            }

            if (command is EePoseCommand eeCmd)
            {
                if (eeCmd.LinearVelocity > MaxEeSpeed)
                    return Task.FromResult(
                        CommandResult.Fail(-1,
                            $"末端线速度 {eeCmd.LinearVelocity}m/s 超过上限 {MaxEeSpeed}m/s"));
            }

            return Task.FromResult<CommandResult>(null);
        }
    }
}
