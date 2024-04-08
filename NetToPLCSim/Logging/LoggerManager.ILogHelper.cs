namespace PLCSimConnector
{
    public static partial class LoggerManager
    {
        /// <summary>
        ///     框架日志辅助器接口。
        /// </summary>
        [System.Reflection.Obfuscation(ApplyToMembers = true, Exclude = true, Feature = "all", StripAfterObfuscation = true)]
        public interface ILogHelper
        {
            /// <summary>
            ///     记录日志。
            /// </summary>
            /// <param name="level">框架日志等级。</param>
            /// <param name="message">日志内容。</param>
            void Log(LogLevel level, object message);
        }
    }
}