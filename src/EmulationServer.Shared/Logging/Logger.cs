using EmulationServer.Shared.Logging.Configuration;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.Shared.Logging.Interfaces;
using EmulationServer.Shared.Logging.Services;

namespace EmulationServer.Shared.Logging;

public static class Logger
{
    private static readonly object SyncRoot = new();
    private static ILogger _logger = new ConsoleLogger();

    public static void Configure(LoggingSettings settings)
    {
        SetLogger(new ConfiguredLogger(settings));
    }

    public static void SetLogger(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        lock (SyncRoot)
        {
            if (_logger is IDisposable disposableLogger)
            {
                disposableLogger.Dispose();
            }

            _logger = logger;
        }
    }

    public static void Write(LogType type, string message, string? category = null)
    {
        lock (SyncRoot)
        {
            _logger.Write(type, message, category);
        }
    }
}
