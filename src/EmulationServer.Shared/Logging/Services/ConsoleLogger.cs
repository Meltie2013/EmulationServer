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
        lock (SyncRoot)
        {
            Console.ForegroundColor = GetColor(type);
            Console.WriteLine(LogMessageFormatter.Format(type, message, category));
            Console.ResetColor();
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
            _ => ConsoleColor.Gray,
        };
    }
}
