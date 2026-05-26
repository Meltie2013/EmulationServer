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
using EmulationServer.Shared.Logging.Formatting;
using EmulationServer.Shared.Logging.Interfaces;

/**
  * File overview: src/EmulationServer.Shared/Logging/Services/ConsoleLogger.cs
  * Documents the ConsoleLogger source file in the shared configuration, logging, and utility support area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Shared.Logging.Services;

/**
  * Owns the console logger behavior for the shared configuration, logging, and utility support layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class ConsoleLogger : ILogger
{
    /**
      * Stores the default sync root value used when the caller does not supply an override.
      * Centralizing the default keeps configuration and packet behavior consistent across the server process.
      */
    private static readonly object SyncRoot = new();

    /**
      * Writes write data to the target packet, stream, or persistent store.
      * The method keeps binary layout and serialization rules centralized for easier packet review and compatibility fixes.
      * Inputs used by this operation: type, message, category.
      */
    public void Write(LogType type, string message, string? category = null)
    {
        IReadOnlyList<string> lines = LogMessageFormatter.FormatLines(
            type,
            message,
            category,
            GetConsoleLineLength());

        lock (SyncRoot)
        {
            Console.ForegroundColor = GetColor(type);

            foreach (string line in lines)
            {
                Console.WriteLine(line);
            }

            Console.ResetColor();
        }
    }


    /**
      * Writes already-formatted lines directly to the console without adding timestamp prefixes.
      * Startup banners use this path so visual output stays compact.
      */
    public void WriteRaw(LogType type, IReadOnlyList<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        lock (SyncRoot)
        {
            Console.ForegroundColor = GetColor(type);

            foreach (string line in lines)
            {
                Console.WriteLine(line);
            }

            Console.ResetColor();
        }
    }

    /**
      * Returns the active console width so long messages can be wrapped before the terminal wraps them in the middle of a word.
      */
    private static int GetConsoleLineLength()
    {
        try
        {
            return Console.IsOutputRedirected
                ? LogMessageFormatter.DefaultMaximumLineLength
                : Math.Max(80, Console.WindowWidth - 1);
        }
        catch (IOException)
        {
            return LogMessageFormatter.DefaultMaximumLineLength;
        }
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of ConsoleLogger and keeps this workflow isolated from the caller.
      */
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
            LogType.SYSTEM => ConsoleColor.White,
            LogType.FUNC => ConsoleColor.DarkGray,
            _ => ConsoleColor.Gray,
        };
    }
}
