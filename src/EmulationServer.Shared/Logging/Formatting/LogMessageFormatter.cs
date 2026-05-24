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
  * File overview: src/EmulationServer.Shared/Logging/Formatting/LogMessageFormatter.cs
  * This file belongs to the logging configuration, formatting, filtering, and output routing portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Shared.Logging.Formatting;

/**
  * Represents the log message formatter component in the logging configuration, formatting, filtering, and output routing area.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public static class LogMessageFormatter
{
    /**
      * Formats runtime values into a stable human-readable message for logging or diagnostics.
      * The method is part of LogMessageFormatter and keeps this workflow isolated from the caller.
      */
    public static string Format(LogType type, string message, string? category = null)
    {
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        return
            $"[{timestamp}] " +
            $"[{type}] " +
            $"{FormatCategory(category, message)}" +
            $"{message}";
    }

    private static string FormatCategory(string? category, string message)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return string.Empty;
        }

        return message.StartsWith(category, StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : $"[{category}] ";
    }
}
