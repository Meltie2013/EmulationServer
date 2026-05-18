using EmulationServer.Shared.Logging.Enums;
using EmulationServer.Shared.Logging.Formatting;
using EmulationServer.Shared.Logging.Interfaces;

namespace EmulationServer.Shared.Logging.Services;

public sealed class ConsoleLogger : ILogger
{
    private static readonly object SyncRoot = new();

    public void Write(LogType type, string message, string? category = null)
    {
        lock (SyncRoot)
        {
            Console.ForegroundColor = GetColor(type);
            Console.WriteLine(LogMessageFormatter.Format(type, message, category));
            Console.ResetColor();
        }
    }

    private static ConsoleColor GetColor(LogType type)
    {
        return type switch
        {
            LogType.SUCCESS => ConsoleColor.Green,
            LogType.WARNING => ConsoleColor.Yellow,
            LogType.FAILED => ConsoleColor.Red,
            LogType.CRITICAL => ConsoleColor.DarkRed,
            LogType.ALERT => ConsoleColor.Magenta,
            LogType.EMERG => ConsoleColor.DarkMagenta,
            LogType.DEBUG => ConsoleColor.Gray,
            LogType.TRACE => ConsoleColor.DarkGray,
            LogType.NETWORK => ConsoleColor.Blue,
            LogType.DATABASE => ConsoleColor.DarkCyan,
            LogType.INFORMATION => ConsoleColor.White,
            LogType.NOTICE => ConsoleColor.Cyan,
            LogType.THREAD => ConsoleColor.DarkYellow,
            _ => ConsoleColor.Gray,
        };
    }
}
