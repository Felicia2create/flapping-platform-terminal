using System.Linq;
using System.Threading.Tasks;
using FPT.Core;

namespace FPT.Business
{
    /// <summary>
    /// 关节限位拦截器 — 检查目标角度是否在允许范围内
    /// </summary>
    public class JointLimitInterceptor : ICommandInterceptor
    {
        public string Name => "JointLimitCheck";

        private readonly double[] _minAngles;
        private readonly double[] _maxAngles;

        /// <param name="minAngles">各关节最小角度（度）</param>
        /// <param name="maxAngles">各关节最大角度（度）</param>
        public JointLimitInterceptor(double[] minAngles, double[] maxAngles)
        {
            _minAngles = minAngles;
            _maxAngles = maxAngles;
        }

        public Task<CommandResult> Intercept(IDeviceCommand command, IDeviceState currentState)
        {
            if (command is JointCommand jointCmd)
            {
                if (jointCmd.TargetAngles == null || jointCmd.TargetAngles.Length == 0)
                    return Task.FromResult(
                        CommandResult.Fail("目标角度为空"));

                var jointCount = jointCmd.TargetAngles.Length;

                for (int i = 0; i < jointCount; i++)
                {
                    var angle = jointCmd.TargetAngles[i];

                    if (i < _minAngles.Length && angle < _minAngles[i])
                        return Task.FromResult(
                            CommandResult.Fail(-1,
                                $"关节 J{i + 1} 超出下限: {angle:F1}° < {_minAngles[i]}°"));

                    if (i < _maxAngles.Length && angle > _maxAngles[i])
                        return Task.FromResult(
                            CommandResult.Fail(-1,
                                $"关节 J{i + 1} 超出上限: {angle:F1}° > {_maxAngles[i]}°"));
                }
            }

            // 通过
            return Task.FromResult<CommandResult>(null);
        }
    }
}
