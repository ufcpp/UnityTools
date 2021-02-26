using System;

namespace CopyDllsAfterBuild
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
    }

    /// <summary>
    /// Logger with level. I don't want refer another packages like Microsoft.Extensions.Logging.
    /// </summary>
    class Logger : ILogger
    {
        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = GetLogger();
                }
                return _instance;
            }
        }
        private static Logger? _instance = null;

        private readonly LogLevel _logLevel;
        private Logger(LogLevel logLevel)
        {
            _logLevel = logLevel;
        }

        private static Logger GetLogger()
        {
            // You can change LogLevel via EnvironmentVariables
            var logLevelEnv = Environment.GetEnvironmentVariable("COPYDLLS_LOGLEVEL");
            var logLevel = logLevelEnv != null && Enum.TryParse<LogLevel>(logLevelEnv, out var parsed)
                ? parsed
                : LogLevel.Information;
            return new Logger(logLevel);
        }

        private bool IsEnabled(LogLevel level) => level >= _logLevel;

        public void LogTrace(string message)
        {
            if (IsEnabled(LogLevel.Trace))
                Console.WriteLine("[trac] " + message);
        }
        public void LogDebug(string message)
        {
            if (IsEnabled(LogLevel.Debug))
                Console.WriteLine("[debg] " + message);
        }
        public void LogInformation(string message)
        {
            if (IsEnabled(LogLevel.Information))
                Console.WriteLine("[info] " + message);
        }
        public void LogWarning(string message)
        {
            if (IsEnabled(LogLevel.Warning))
                Console.WriteLine("[warn] " + message);
        }
        public void LogError(string message)
        {
            if (IsEnabled(LogLevel.Error))
                Console.WriteLine("[erro] " + message);
        }
        public void LogCritical(string message)
        {
            if (IsEnabled(LogLevel.Critical))
                Console.WriteLine("[crit] " + message);
        }
    }
}
