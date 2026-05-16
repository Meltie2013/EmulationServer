
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Shared.Logging.Interfaces;

public interface ILogger
{
    void Write(LogType type, string message, string? category = null);
}
