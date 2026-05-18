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

using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/EmulationServer.Shared/Logging/Configuration/LoggingSettings.cs
  * This file belongs to the logging configuration, formatting, filtering, and output routing portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Shared.Logging.Configuration;

/**
  * Represents the logging settings component in the logging configuration, formatting, filtering, and output routing area.
  * It keeps configuration values grouped by responsibility and prevents unrelated server code from reading raw INI keys.
  */
public sealed class LoggingSettings
{
    /**
      * Gets or stores the server name value used by LoggingSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string ServerName { get; init; } = "EmulationServer";

    /**
      * Gets or stores the output value used by LoggingSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public LogOutputMode Output { get; init; } = LogOutputMode.Console;

    /**
      * Gets or stores the log folder value used by LoggingSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string LogFolder { get; init; } = Path.Combine(AppContext.BaseDirectory, "logs");

    /**
      * Gets or stores the file name value used by LoggingSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string FileName { get; init; } = "EmulationServer.log";

    /**
      * Gets or stores the enabled types value used by LoggingSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public IReadOnlySet<LogType> EnabledTypes { get; init; } = Enum.GetValues<LogType>().ToHashSet();

    /**
      * Performs the is enabled operation for LoggingSettings.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    public bool IsEnabled(LogType type)
    {
        return EnabledTypes.Contains(type);
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of LoggingSettings and keeps this workflow isolated from the caller.
      */
    public string GetLogFilePath()
    {
        return Path.Combine(LogFolder, FileName);
    }

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of LoggingSettings and keeps this workflow isolated from the caller.
      */
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
