
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Shared.Logging.Models;

public sealed class LogMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public LogType Type { get; init; }

    public string Message { get; init; } = string.Empty;

    public string? Category { get; init; }
}
