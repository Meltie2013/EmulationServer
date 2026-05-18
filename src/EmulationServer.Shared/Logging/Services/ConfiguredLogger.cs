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

namespace EmulationServer.Shared.Logging.Services;

public sealed class ConfiguredLogger : ILogger, IDisposable
{
    private readonly object _syncRoot = new();
    private readonly LoggingSettings _settings;
    private readonly StreamWriter? _fileWriter;
    private bool _disposed;

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
