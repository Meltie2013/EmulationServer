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
using EmulationServer.Shared.Logging.Interfaces;
using EmulationServer.Shared.Logging.Services;

/**
  * File overview: src/EmulationServer.Shared/Logging/Logger.cs
  * Documents the Logger source file in the shared configuration, logging, and utility support area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Shared.Logging;

/**
  * Owns the logger behavior for the shared configuration, logging, and utility support layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public static class Logger
{
    /**
      * Stores the default sync root value used when the caller does not supply an override.
      * Centralizing the default keeps configuration and packet behavior consistent across the server process.
      */
    private static readonly object SyncRoot = new();
    /**
      * Holds the private logger state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private static ILogger _logger = new ConsoleLogger();

    /**
      * Applies configuration to shared runtime services before they are used by the server.
      * The method is part of Logger and keeps this workflow isolated from the caller.
      */
    public static void Configure(LoggingSettings settings)
    {
        SetLogger(new ConfiguredLogger(settings));
    }

    /**
      * Updates the stored value after validating that the new value is safe to use.
      * The method is part of Logger and keeps this workflow isolated from the caller.
      */
    public static void SetLogger(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        lock (SyncRoot)
        {
            if (_logger is IDisposable disposableLogger)
            {
                disposableLogger.Dispose();
            }

            _logger = logger;
        }
    }

    /**
      * Writes write data to the target packet, stream, or persistent store.
      * The method keeps binary layout and serialization rules centralized for easier packet review and compatibility fixes.
      * Inputs used by this operation: type, message, category.
      */
    public static void Write(LogType type, string message, string? category = null)
    {
        lock (SyncRoot)
        {
            _logger.Write(type, message, category);
        }
    }

    /**
      * Writes a short server banner without repeating timestamps on every banner line.
      * Banners are intentionally kept out of the normal formatter so startup output stays readable.
      */
    public static void WriteBanner(string serverName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName);
        WriteRaw(LogType.NOTICE, BuildBannerLines(serverName));
    }

    /**
      * Writes already-formatted output directly through the active logger.
      * This is reserved for banners and visual separators; regular status messages should use Write.
      */
    public static void WriteRaw(LogType type, IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        string[] outputLines = lines
            .Select(line => line ?? string.Empty)
            .ToArray();

        lock (SyncRoot)
        {
            _logger.WriteRaw(type, outputLines);
        }
    }

    /**
      * Builds the common startup banner used by every executable server.
      */
    private static IReadOnlyList<string> BuildBannerLines(string serverName)
    {
        const int width = 81;
        string title = $":: {serverName} ::";

        return
        [
            @" _____                 _       _   _              ____                           ",
            @"| ____|_ __ ___  _   _| | __ _| |_(_) ___  _ __  / ___|  ___ _ ____   _____ _ __ ",
            @"|  _| | '_ ` _ \| | | | |/ _` | __| |/ _ \| '_ \ \___ \ / _ \ '__\ \ / / _ \ '__|",
            @"| |___| | | | | | |_| | | (_| | |_| | (_) | | | | ___) |  __/ |   \ V /  __/ |   ",
            @"|_____|_| |_| |_|\__,_|_|\__,_|\__|_|\___/|_| |_||____/ \___|_|    \_/ \___|_|   ",
            string.Empty.PadRight(width),
            Center(title, width),
        ];
    }

    /**
      * Centers one piece of text inside a fixed-width banner row.
      */
    private static string Center(string value, int width)
    {
        if (value.Length >= width)
        {
            return value;
        }

        int leftPadding = (width - value.Length) / 2;
        return new string(' ', leftPadding) + value;
    }
}
