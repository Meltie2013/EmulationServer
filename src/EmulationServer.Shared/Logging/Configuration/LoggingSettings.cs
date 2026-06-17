//
// Copyright (C) 2026 Emulation Server Project
//
// This program is free software. You can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation. either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY. Without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// File: src/EmulationServer.Shared/Logging/Configuration/LoggingSettings.cs
// Purpose: Contains logging settings code for the shared infrastructure, logging, timing, and cross-service utility layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Shared.Logging.Configuration;

// Type: LoggingSettings
// Purpose: Provides logging settings behavior for the shared infrastructure, logging, timing, and cross-service utility layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class LoggingSettings
{

    // Property: Gets or sets the server name value used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: server name value exposed by the owning type.
    public string ServerName { get; init; } = "EmulationServer";

    // Property: Gets or sets the output value used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: output value exposed by the owning type.
    public LogOutputMode Output { get; init; } = LogOutputMode.Console;

    // Method: Combine
    // Purpose: Executes the combine operation for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - BaseDirectory: Base directory value supplied by the caller for this operation.
    // - logs: Logs value supplied by the caller for this operation.
    // Returns: Returns the string log folder { get; init; } = path. value produced by this operation.
    // Notes: This keeps the operation scoped to LoggingSettings so callers do not duplicate validation, protocol, or persistence rules.
    public string LogFolder { get; init; } = Path.Combine(AppContext.BaseDirectory, "logs");

    // Property: Gets or sets the file name value used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: file name value exposed by the owning type.
    public string FileName { get; init; } = "EmulationServer.log";

    // Method: LogType
    // Purpose: Executes the log type operation for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters: none.
    // Returns: Returns the I read only set enabled types { get; init; } = enum.get values< value produced by this operation.
    // Notes: This keeps the operation scoped to LoggingSettings so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlySet<LogType> EnabledTypes { get; init; } = Enum.GetValues<LogType>().ToHashSet();

    // Method: IsEnabled
    // Purpose: Validates or evaluates is enabled rules for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - type: Type value supplied by the caller for this operation.
    // Returns: Returns true when is enabled succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to LoggingSettings so callers do not duplicate validation, protocol, or persistence rules.
    public bool IsEnabled(LogType type)
    {
        return EnabledTypes.Contains(type);
    }

    // Method: GetLogFilePath
    // Purpose: Retrieves get log file path data for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters: none.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to LoggingSettings so callers do not duplicate validation, protocol, or persistence rules.
    public string GetLogFilePath()
    {
        return Path.Combine(LogFolder, FileName);
    }

    // Method: Validate
    // Purpose: Validates or evaluates validate rules for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to LoggingSettings so callers do not duplicate validation, protocol, or persistence rules.
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
