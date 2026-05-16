
namespace EmulationServer.Tests.Database;

[AttributeUsage(AttributeTargets.Method)]
public sealed class DatabaseIntegrationFactAttribute : FactAttribute
{
    private const string EnabledValue = "true";
    private const string EnvironmentVariableName = "EMULATIONSERVER_RUN_DATABASE_TESTS";

    public DatabaseIntegrationFactAttribute()
    {
        string? enabled = Environment.GetEnvironmentVariable(EnvironmentVariableName);

        if (!string.Equals(enabled, EnabledValue, StringComparison.OrdinalIgnoreCase))
        {
            Skip = $"Set {EnvironmentVariableName}=true to run database integration tests.";
        }
    }
}
