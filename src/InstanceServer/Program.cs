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
// File: src/InstanceServer/Program.cs
// Purpose: Contains program code for the instance server runtime, dungeon-map ownership, and internal-service coordination.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.InstanceServer.Configuration;
using EmulationServer.InstanceServer.Core;
using EmulationServer.Shared.Configuration;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.Shared.Threading;

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
    string configurationPath = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "instanceserver.ini");

    InstanceServerSettings settings = InstanceServerConfigurationLoader.Load(configurationPath);

    Logger.Configure(settings.Logging);

    Logger.WriteBanner("Instance Server");

    RuntimeConcurrencyConfigurator.ConfigureForServer("InstanceServer");

    await using InstanceServer server = new(settings);

    await server.StartAsync(cancellation.Token);
}

catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    Logger.Write(LogType.INFORMATION, "Shutdown requested. Stopping InstanceServer...", "Program");
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
