namespace FPT.Core
{
    /// <summary>
    /// 指令执行结果
    /// </summary>
    public class CommandResult
    {
        /// <summary> 是否执行成功 </summary>
        public bool Success { get; set; }

        /// <summary> 错误码（0 = 无错误）</summary>
        public int ErrorCode { get; set; }

        /// <summary> 结果描述 / 错误信息 </summary>
        public string Message { get; set; }

        /// <summary> 指令类型标识（用于日志追踪）</summary>
        public string CommandType { get; set; }

        public static CommandResult Ok(string message = null)
            => new CommandResult
            {
                Success = true,
                ErrorCode = 0,
                Message = message ?? "OK",
            };

        public static CommandResult Fail(int errorCode, string message)
            => new CommandResult
            {
                Success = false,
                ErrorCode = errorCode,
                Message = message,
            };

        public static CommandResult Fail(string message)
            => new CommandResult
            {
                Success = false,
                ErrorCode = -1,
                Message = message,
            };
    }
}
