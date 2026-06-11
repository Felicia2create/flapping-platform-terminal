using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FPT.Core;

namespace FPT.Business
{
    /// <summary>
    /// 设备协调器 — 简化版（按需扩展）
    /// 管理多设备的协同任务序列
    /// </summary>
    public class DeviceCoordinator
    {
        private readonly DeviceManager _deviceManager;

        public DeviceCoordinator(DeviceManager deviceManager)
        {
            _deviceManager = deviceManager
                ?? throw new ArgumentNullException(nameof(deviceManager));
        }

        /// <summary>
        /// 所有设备状态快照
        /// </summary>
        public IReadOnlyDictionary<string, IDeviceState> AllStates
            => _deviceManager.AllStates;

        /// <summary>
        /// 任意设备状态变更事件
        /// </summary>
        public event Action<string, IDeviceState> OnAnyDeviceStateChanged;

        public DeviceCoordinator Subscribe()
        {
            _deviceManager.OnAnyDeviceStateChanged += state =>
            {
                OnAnyDeviceStateChanged?.Invoke(state.DeviceId, state);
            };
            return this;
        }

        /// <summary>
        /// 按顺序执行设备任务序列
        /// 每步等待完成条件或超时
        /// </summary>
        public async Task<CoordinatedResult> ExecuteSequence(DeviceSequence sequence)
        {
            var results = new List<CommandResult>();

            for (int i = 0; i < sequence.Steps.Count; i++)
            {
                var step = sequence.Steps[i];
                var driver = _deviceManager.GetDriver(step.DeviceId);

                if (driver == null)
                {
                    results.Add(CommandResult.Fail(
                        $"步骤 {i + 1}: 设备 {step.DeviceId} 未找到"));
                    return new CoordinatedResult { Success = false, StepResults = results };
                }

                // 发送指令
                var result = await driver.ExecuteCommand(step.Command);
                results.Add(result);

                if (!result.Success)
                {
                    return new CoordinatedResult { Success = false, StepResults = results };
                }

                // 等待完成条件
                if (step.CompletionCondition != null)
                {
                    var completed = false;
                    var startTime = DateTime.Now;

                    while (!completed)
                    {
                        await Task.Delay(100);

                        var state = driver.CurrentState;
                        completed = step.CompletionCondition(state);

                        if (!completed && (DateTime.Now - startTime) > step.Timeout)
                        {
                            results.Add(CommandResult.Fail(
                                $"步骤 {i + 1}: 等待完成超时"));
                            return new CoordinatedResult
                                { Success = false, StepResults = results };
                        }
                    }
                }
            }

            return new CoordinatedResult { Success = true, StepResults = results };
        }
    }

    /// <summary>
    /// 协调执行结果
    /// </summary>
    public class CoordinatedResult
    {
        public bool Success { get; set; }
        public List<CommandResult> StepResults { get; set; }
            = new List<CommandResult>();
    }

    /// <summary>
    /// 设备任务序列
    /// </summary>
    public class DeviceSequence
    {
        public string Name { get; set; }
        public List<SequenceStep> Steps { get; set; }
            = new List<SequenceStep>();
    }

    /// <summary>
    /// 序列中的单步
    /// </summary>
    public class SequenceStep
    {
        /// <summary> 目标设备 ID </summary>
        public string DeviceId { get; set; }

        /// <summary> 要执行的指令 </summary>
        public IDeviceCommand Command { get; set; }

        /// <summary> 完成条件（可选）</summary>
        public Func<IDeviceState, bool> CompletionCondition { get; set; }

        /// <summary> 单步超时时间 </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}
