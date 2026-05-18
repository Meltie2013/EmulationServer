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
  * This file belongs to the logging configuration, formatting, filtering, and output routing portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Shared.Logging;

/**
  * Represents the logger component in the logging configuration, formatting, filtering, and output routing area.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public static class Logger
{
    private static readonly object SyncRoot = new();
    /**
      * Stores the logger dependency or runtime value for Logger.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
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
      * Writes the supplied data to the target destination using the project protocol or file format.
      * The method is part of Logger and keeps this workflow isolated from the caller.
      */
    public static void Write(LogType type, string message, string? category = null)
    {
        lock (SyncRoot)
        {
            _logger.Write(type, message, category);
        }
    }
}
