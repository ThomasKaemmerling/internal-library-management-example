using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace Backend.Logging;

public sealed class DailyRollingFileLoggerProvider : ILoggerProvider
{
    private readonly string _directoryPath;
    private readonly string _categoryPrefix;
    private readonly object _sync = new();

    public DailyRollingFileLoggerProvider(string directoryPath, string categoryPrefix)
    {
        _directoryPath = directoryPath;
        _categoryPrefix = categoryPrefix;
        Directory.CreateDirectory(_directoryPath);
    }

    public ILogger CreateLogger(string categoryName)
    {
        if (!categoryName.StartsWith(_categoryPrefix, StringComparison.Ordinal))
        {
            return NullLogger.Instance;
        }

        return new DailyRollingFileLogger(_directoryPath, categoryName, _sync);
    }

    public void Dispose()
    {
    }

    private sealed class DailyRollingFileLogger : ILogger
    {
        private readonly string _directoryPath;
        private readonly string _categoryName;
        private readonly object _sync;

        public DailyRollingFileLogger(string directoryPath, string categoryName, object sync)
        {
            _directoryPath = directoryPath;
            _categoryName = categoryName;
            _sync = sync;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            if (string.IsNullOrWhiteSpace(message) && exception is null)
            {
                return;
            }

            var timestamp = DateTime.Now;
            var filePath = Path.Combine(_directoryPath, $"parser-{timestamp:yyyy-MM-dd}.log");
            var builder = new StringBuilder()
                .Append(timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                .Append(' ')
                .Append('[')
                .Append(logLevel)
                .Append("] ")
                .Append(_categoryName)
                .Append(": ")
                .Append(message);

            if (exception is not null)
            {
                builder.AppendLine();
                builder.Append(exception);
            }

            builder.AppendLine();

            lock (_sync)
            {
                File.AppendAllText(filePath, builder.ToString(), Encoding.UTF8);
            }
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}