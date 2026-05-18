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
