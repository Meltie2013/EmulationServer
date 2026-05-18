using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Shared.Logging.Formatting;

public static class LogMessageFormatter
{
    public static string Format(LogType type, string message, string? category = null)
    {
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        return
            $"[{timestamp}] " +
            $"[{type}] " +
            $"{(category is not null ? $"[{category}] " : string.Empty)}" +
            $"{message}";
    }
}
