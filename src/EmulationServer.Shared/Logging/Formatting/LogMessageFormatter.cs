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
  * Documents the LogMessageFormatter source file in the shared configuration, logging, and utility support area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Shared.Logging.Formatting;

/**
  * Owns the log message formatter behavior for the shared configuration, logging, and utility support layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
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

    /**
      * Performs the format category operation for the shared configuration, logging, and utility support workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: category, message.
      */
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
