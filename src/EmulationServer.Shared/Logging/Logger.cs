
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.Shared.Logging.Interfaces;
using EmulationServer.Shared.Logging.Services;

namespace EmulationServer.Shared.Logging;

public static class Logger
{
    private static ILogger _logger = new ConsoleLogger();

    public static void SetLogger(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public static void Write(LogType type, string message, string? category = null)
    {
        _logger.Write(type, message, category);
    }
}
