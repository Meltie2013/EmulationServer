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
  * This file belongs to the project runtime logic and supporting data models portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

using CancellationTokenSource cancellation = new();


Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;

    if (!cancellation.IsCancellationRequested)
    {
        cancellation.Cancel();
    }
};

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
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    Logger.Write(LogType.INFORMATION, "Shutdown requested. Stopping MapServer...", nameof(Program));
}
catch (ConfigurationException exception)
{
    Logger.Write(LogType.CRITICAL, $"Configuration error: {exception.Message}");
    Environment.ExitCode = 1;
}
catch (Exception exception)
{
    Logger.Write(LogType.CRITICAL, exception.ToString());
    Environment.ExitCode = 1;
}
