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
// File: src/EmulationServer.Shared/Logging/Logger.cs
// Purpose: Contains logger code for the shared infrastructure, logging, timing, and cross-service utility layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Shared.Logging.Configuration;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.Shared.Logging.Interfaces;
using EmulationServer.Shared.Logging.Services;

namespace EmulationServer.Shared.Logging;

// Type: Logger
// Purpose: Provides logger behavior for the shared infrastructure, logging, timing, and cross-service utility layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class Logger
{

    private static readonly object SyncRoot = new();

    // Method: ConsoleLogger
    // Purpose: Executes the console logger operation for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters: none.
    // Returns: Returns the I logger logger = new value produced by this operation.
    // Notes: This keeps the operation scoped to Logger so callers do not duplicate validation, protocol, or persistence rules.
    private static ILogger _logger = new ConsoleLogger();

    // Method: Configure
    // Purpose: Executes the configure operation for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - settings: Settings values that control how this operation should run.
    // Returns: none.
    // Notes: This keeps the operation scoped to Logger so callers do not duplicate validation, protocol, or persistence rules.
    public static void Configure(LoggingSettings settings)
    {
        SetLogger(new ConfiguredLogger(settings));
    }

    // Method: SetLogger
    // Purpose: Applies set logger changes for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - logger: Logger value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to Logger so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: Write
    // Purpose: Builds or writes write output for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - type: Type value supplied by the caller for this operation.
    // - message: Message value supplied by the caller for this operation.
    // - category: Category value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to Logger so callers do not duplicate validation, protocol, or persistence rules.
    public static void Write(LogType type, string message, string? category = null)
    {
        lock (SyncRoot)
        {
            _logger.Write(type, message, category);
        }
    }

    // Method: WriteBanner
    // Purpose: Builds or writes write banner output for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - serverName: Server name value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to Logger so callers do not duplicate validation, protocol, or persistence rules.
    public static void WriteBanner(string serverName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName);
        WriteRaw(LogType.NOTICE, BuildBannerLines(serverName));
    }

    // Method: WriteRaw
    // Purpose: Builds or writes write raw output for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - type: Type value supplied by the caller for this operation.
    // - lines: Lines value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to Logger so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: BuildBannerLines
    // Purpose: Builds or writes build banner lines output for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - serverName: Server name value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to Logger so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: Center
    // Purpose: Executes the center operation for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // - width: Width value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to Logger so callers do not duplicate validation, protocol, or persistence rules.
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
