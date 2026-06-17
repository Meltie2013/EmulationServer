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
// File: src/EmulationServer.Shared/Logging/Services/ConsoleLogger.cs
// Purpose: Contains console logger code for the shared infrastructure, logging, timing, and cross-service utility layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Shared.Logging.Enums;
using EmulationServer.Shared.Logging.Formatting;
using EmulationServer.Shared.Logging.Interfaces;

namespace EmulationServer.Shared.Logging.Services;

// Type: ConsoleLogger
// Purpose: Provides console logger behavior for the shared infrastructure, logging, timing, and cross-service utility layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class ConsoleLogger : ILogger
{

    private static readonly object SyncRoot = new();

    // Method: Write
    // Purpose: Builds or writes write output for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - type: Type value supplied by the caller for this operation.
    // - message: Message value supplied by the caller for this operation.
    // - category: Category value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ConsoleLogger so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: WriteRaw
    // Purpose: Builds or writes write raw output for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - type: Type value supplied by the caller for this operation.
    // - lines: Lines value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ConsoleLogger so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: GetConsoleLineLength
    // Purpose: Retrieves get console line length data for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters: none.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to ConsoleLogger so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: GetColor
    // Purpose: Retrieves get color data for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - type: Type value supplied by the caller for this operation.
    // Returns: Returns the console color value produced by this operation.
    // Notes: This keeps the operation scoped to ConsoleLogger so callers do not duplicate validation, protocol, or persistence rules.
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
