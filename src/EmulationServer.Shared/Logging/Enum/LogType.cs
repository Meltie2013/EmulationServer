
namespace EmulationServer.Shared.Logging.Enums;

public enum LogType
{
    NETWORK,       // Network code debugging
    DEBUG,         // Packet processing
    INFORMATION,   // General information
    USER,          // User actions
    SUCCESS,       // Successful operations
    WARNING,       // Warning conditions
    FAILED,        // Processing errors
    CRITICAL,      // Application errors
    DATABASE,      // Database operations/errors
    ALERT,         // Immediate action required
    EMERG,         // System unusable
    FUNC,          // Function tracing
    NOTICE,        // Significant condition
    THREAD,        // Thread tracing
    TRACE          // Fine-grained debugging
}
