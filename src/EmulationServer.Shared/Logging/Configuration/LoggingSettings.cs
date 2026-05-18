using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Shared.Logging.Configuration;

public sealed class LoggingSettings
{
    public string ServerName { get; init; } = "EmulationServer";

    public LogOutputMode Output { get; init; } = LogOutputMode.Console;

    public string LogFolder { get; init; } = Path.Combine(AppContext.BaseDirectory, "logs");

    public string FileName { get; init; } = "EmulationServer.log";

    public IReadOnlySet<LogType> EnabledTypes { get; init; } = Enum.GetValues<LogType>().ToHashSet();

    public bool IsEnabled(LogType type)
    {
        return EnabledTypes.Contains(type);
    }

    public string GetLogFilePath()
    {
        return Path.Combine(LogFolder, FileName);
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ServerName))
        {
            throw new InvalidOperationException("Logging server name is required.");
        }

        if (Output is LogOutputMode.File or LogOutputMode.Both)
        {
            if (string.IsNullOrWhiteSpace(LogFolder))
            {
                throw new InvalidOperationException("Logging log folder is required when file logging is enabled.");
            }

            if (string.IsNullOrWhiteSpace(FileName))
            {
                throw new InvalidOperationException("Logging file name is required when file logging is enabled.");
            }
        }
    }
}
