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

using EmulationServer.Shared.Logging.Configuration;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.Shared.Logging.Formatting;
using EmulationServer.Shared.Logging.Interfaces;

/**
  * File overview: src/EmulationServer.Shared/Logging/Services/ConfiguredLogger.cs
  * Documents the ConfiguredLogger source file in the shared configuration, logging, and utility support area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Shared.Logging.Services;

/**
  * Applies log filtering and output routing so messages can go to console, file, or both.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public sealed class ConfiguredLogger : ILogger, IDisposable
{
    /**
      * Holds the private sync root state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly object _syncRoot = new();
    /**
      * Holds the private settings state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly LoggingSettings _settings;
    /**
      * Holds the private file writer state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly StreamWriter? _fileWriter;
    /**
      * Holds the private disposed state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private bool _disposed;

    /**
      * Initializes a new ConfiguredLogger instance with the dependencies required by the shared configuration, logging, and utility support workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: settings.
      */
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

    /**
      * Writes write data to the target packet, stream, or persistent store.
      * The method keeps binary layout and serialization rules centralized for easier packet review and compatibility fixes.
      * Inputs used by this operation: type, message, category.
      */
    public void Write(LogType type, string message, string? category = null)
    {
        if (_disposed || !_settings.IsEnabled(type))
        {
            return;
        }

        string formattedMessage = LogMessageFormatter.Format(type, message, category);

        lock (_syncRoot)
        {
            if (_settings.Output is LogOutputMode.Console or LogOutputMode.Both)
            {
                Console.ForegroundColor = GetColor(type);
                Console.WriteLine(formattedMessage);
                Console.ResetColor();
            }

            if (_fileWriter is not null)
            {
                _fileWriter.WriteLine(formattedMessage);
            }
        }
    }

    /**
      * Stops the dispose workflow and releases owned runtime resources in a controlled order.
      * Shutdown logic is centralized to avoid dangling connections, incomplete saves, or partially registered services.
      */
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

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of ConfiguredLogger and keeps this workflow isolated from the caller.
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
