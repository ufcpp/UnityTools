using System;

namespace CopyDllsAfterBuildLocalTool
{
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Critical = 5,
    }

    interface ILogger
    {
        void LogCritical(string message);
        void LogDebug(string message);
        void LogError(string message);
        void LogInformation(string message);
        void LogTrace(string message);
        void LogWarning(string message);
        void LogInformationIfNotDebug(string message);
    }

    // I don't want refer another packages like Microsoft.Extensions.Logging, just create simple logger.
    /// <summary>
    /// Singleton Logger with level. Use logger with <see cref="Logger.Instance"/>
    /// </summary>
    class Logger : ILogger
    {
        private static readonly LogLevel defaultLogLevel = LogLevel.Information;
        // add header when logLevel is equals or above this.
        private static readonly LogLevel headerLogLevel = LogLevel.Debug;
        private static readonly string[] logHeaders = new[]
        {
            "[trac] ",
            "[debg] ",
            "[info] ",
            "[warn] ",
            "[erro] ",
            "[crit] ",
        };
        /// <summary>
        /// Singleton instance of <see cref="Logger">
        /// </summary>
        public static Logger Instance => _instance ??= GetLogger();
        private static Logger? _instance = null;

        private readonly LogLevel _logLevel;
        private Logger(LogLevel logLevel) => _logLevel = logLevel;

        private static Logger GetLogger()
        {
            // You can change LogLevel via EnvironmentVariables
            var logLevelEnv = Environment.GetEnvironmentVariable("COPYDLLS_LOGLEVEL");
            var logLevel = logLevelEnv != null && Enum.TryParse<LogLevel>(logLevelEnv, out var level)
                ? level
                : defaultLogLevel;
            return new Logger(logLevel);
        }

        private bool IsEnabled(LogLevel level) => level >= _logLevel;
        private string GetPrefix(LogLevel logLevel) => headerLogLevel >= _logLevel ? $"{logHeaders[(int)logLevel]}" : "";

        public void LogTrace(string message) => Log(LogLevel.Trace, message);
        public void LogDebug(string message) => Log(LogLevel.Debug, message);
        public void LogInformation(string message) => Log(LogLevel.Information, message);
        public void LogWarning(string message) => Log(LogLevel.Warning, message);
        public void LogError(string message) => Log(LogLevel.Error, message);
        public void LogCritical(string message) => Log(LogLevel.Trace, message);
        public void LogInformationIfNotDebug(string message)
        {
            if (LogLevel.Debug <= _logLevel)
            {
                LogInformation(message);
            }
        }
        public void Log(LogLevel logLevel, string message)
        {
            // no message formatter or gc friendly, just control level is enough.
            if (IsEnabled(logLevel))
            {
                var prefix = GetPrefix(logLevel);
                Console.WriteLine(prefix + message);
            }
        }
    }
}
