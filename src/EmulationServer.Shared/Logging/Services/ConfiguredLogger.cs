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
// File: src/EmulationServer.Shared/Logging/Services/ConfiguredLogger.cs
// Purpose: Contains configured logger code for the shared infrastructure, logging, timing, and cross-service utility layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Shared.Logging.Configuration;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.Shared.Logging.Formatting;
using EmulationServer.Shared.Logging.Interfaces;

namespace EmulationServer.Shared.Logging.Services;

// Type: ConfiguredLogger
// Purpose: Provides configured logger behavior for the shared infrastructure, logging, timing, and cross-service utility layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class ConfiguredLogger : ILogger, IDisposable
{

    private readonly object _syncRoot = new();

    // Field: Stores the settings state used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: current settings backing value maintained by the owning type.
    private readonly LoggingSettings _settings;

    // Field: Stores the file writer state used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: current file writer backing value maintained by the owning type.
    private readonly StreamWriter? _fileWriter;

    // Field: Stores the disposed state used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: current disposed backing value maintained by the owning type.
    private bool _disposed;

    // Constructor: ConfiguredLogger
    // Purpose: Initializes a new ConfiguredLogger instance with dependencies and values required by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - settings: Settings values that control how this operation should run.
    // Returns: none.
    // Notes: This keeps the operation scoped to ConfiguredLogger so callers do not duplicate validation, protocol, or persistence rules.
    public ConfiguredLogger(LoggingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _settings = settings;

        if (settings.Output is LogOutputMode.File or LogOutputMode.Both)
        {
            Directory.CreateDirectory(settings.LogFolder);

            FileStream stream = new(
                settings.GetLogFilePath(),
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read);

            _fileWriter = new StreamWriter(stream)
            {
                AutoFlush = true,
            };
        }
    }

    // Method: Write
    // Purpose: Builds or writes write output for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - type: Type value supplied by the caller for this operation.
    // - message: Message value supplied by the caller for this operation.
    // - category: Category value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ConfiguredLogger so callers do not duplicate validation, protocol, or persistence rules.
    public void Write(LogType type, string message, string? category = null)
    {
        if (_disposed || !_settings.IsEnabled(type))
        {
            return;
        }

        DateTime timestamp = DateTime.UtcNow;

        lock (_syncRoot)
        {
            if (_settings.Output is LogOutputMode.Console or LogOutputMode.Both)
            {
                IReadOnlyList<string> consoleLines = LogMessageFormatter.FormatLines(
                    type,
                    message,
                    category,
                    GetConsoleLineLength(),
                    timestamp);

                Console.ForegroundColor = GetColor(type);

                foreach (string line in consoleLines)
                {
                    Console.WriteLine(line);
                }

                Console.ResetColor();
            }

            if (_fileWriter is not null)
            {
                IReadOnlyList<string> fileLines = LogMessageFormatter.FormatLines(
                    type,
                    message,
                    category,
                    LogMessageFormatter.DefaultMaximumLineLength,
                    timestamp);

                foreach (string line in fileLines)
                {
                    _fileWriter.WriteLine(line);
                }
            }
        }
    }

    // Method: WriteRaw
    // Purpose: Builds or writes write raw output for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - type: Type value supplied by the caller for this operation.
    // - lines: Lines value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ConfiguredLogger so callers do not duplicate validation, protocol, or persistence rules.
    public void WriteRaw(LogType type, IReadOnlyList<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (_disposed || !_settings.IsEnabled(type))
        {
            return;
        }

        lock (_syncRoot)
        {
            if (_settings.Output is LogOutputMode.Console or LogOutputMode.Both)
            {
                Console.ForegroundColor = GetColor(type);

                foreach (string line in lines)
                {
                    Console.WriteLine(line);
                }

                Console.ResetColor();
            }

            if (_fileWriter is not null)
            {
                foreach (string line in lines)
                {
                    _fileWriter.WriteLine(line);
                }
            }
        }
    }

    // Method: Dispose
    // Purpose: Controls the dispose lifecycle step for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to ConfiguredLogger so callers do not duplicate validation, protocol, or persistence rules.
    public void Dispose()
    {
        lock (_syncRoot)
        {
            if (_disposed)
            {
                return;
            }

            _fileWriter?.Dispose();
            _disposed = true;
        }
    }

    // Method: GetConsoleLineLength
    // Purpose: Retrieves get console line length data for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters: none.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to ConfiguredLogger so callers do not duplicate validation, protocol, or persistence rules.
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
    // Notes: This keeps the operation scoped to ConfiguredLogger so callers do not duplicate validation, protocol, or persistence rules.
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
            LogType.SYSTEM => ConsoleColor.Cyan,
            LogType.FUNC => ConsoleColor.DarkGray,
            _ => ConsoleColor.Gray,
        };
    }
}
