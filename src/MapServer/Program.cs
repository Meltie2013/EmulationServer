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

using EmulationServer.MapServer.Configuration;
using EmulationServer.MapServer.Core;
using EmulationServer.Shared.Configuration;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;


/**
 * File overview: src/MapServer/Program.cs
 * Documents the Program source file for the map server entry point.
 * This top-level entry point handles startup argument selection, configuration loading, cancellation, logging, and controlled process exit behavior with normal comments instead of XML documentation.
 */

// Create a shared cancellation source so Ctrl+C and shutdown paths use the same token.
using CancellationTokenSource cancellation = new();


// Convert Ctrl+C into cooperative cancellation instead of allowing the process to terminate abruptly.
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;

    if (!cancellation.IsCancellationRequested)
    {
        cancellation.Cancel();
    }
};

// Run startup inside a guarded block so configuration and runtime failures are logged before the process exits.
try
{
    string configurationPath = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "mapserver.ini");

    MapServerSettings settings = MapServerConfigurationLoader.Load(configurationPath);

    Logger.Configure(settings.Logging);

    Logger.Write(LogType.NOTICE, @" _____                 _       _   _              ____                           ");
    Logger.Write(LogType.NOTICE, @"| ____|_ __ ___  _   _| | __ _| |_(_) ___  _ __  / ___|  ___ _ ____   _____ _ __ ");
    Logger.Write(LogType.NOTICE, @"|  _| | '_ ` _ \| | | | |/ _` | __| |/ _ \| '_ \ \___ \ / _ \ '__\ \ / / _ \ '__|");
    Logger.Write(LogType.NOTICE, @"| |___| | | | | | |_| | | (_| | |_| | (_) | | | | ___) |  __/ |   \ V /  __/ |   ");
    Logger.Write(LogType.NOTICE, @"|_____|_| |_| |_|\__,_|_|\__,_|\__|_|\___/|_| |_||____/ \___|_|    \_/ \___|_|   ");
    Logger.Write(LogType.NOTICE, @"                                                                                 ");
    Logger.Write(LogType.NOTICE, @"                          :: Map Server ::");

    await using MapServer server = new(settings);

    await server.StartAsync(cancellation.Token);
}
// Treat cancellation from the shared shutdown token as a normal operator-requested stop.
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    Logger.Write(LogType.INFORMATION, "Shutdown requested. Stopping MapServer...", nameof(Program));
}
// Configuration errors are reported as critical startup failures with a non-zero exit code.
catch (ConfigurationException exception)
{
    Logger.Write(LogType.CRITICAL, $"Configuration error: {exception.Message}");
    Environment.ExitCode = 1;
}
// Unexpected failures are logged with full details so startup and runtime crashes can be diagnosed.
catch (Exception exception)
{
    Logger.Write(LogType.CRITICAL, exception.ToString());
    Environment.ExitCode = 1;
}
