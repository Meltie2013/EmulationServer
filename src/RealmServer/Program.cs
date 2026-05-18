
using EmulationServer.RealmServer.Configuration;
using EmulationServer.RealmServer.Core;
using EmulationServer.Shared.Configuration;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

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
    string configurationPath = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "realmserver.ini");

    RealmServerSettings settings = RealmServerConfigurationLoader.Load(configurationPath);

    Logger.Configure(settings.Logging);

    Logger.Write(LogType.NOTICE, @" _____                 _       _   _              ____                           ");
    Logger.Write(LogType.NOTICE, @"| ____|_ __ ___  _   _| | __ _| |_(_) ___  _ __  / ___|  ___ _ ____   _____ _ __ ");
    Logger.Write(LogType.NOTICE, @"|  _| | '_ ` _ \| | | | |/ _` | __| |/ _ \| '_ \ \___ \ / _ \ '__\ \ / / _ \ '__|");
    Logger.Write(LogType.NOTICE, @"| |___| | | | | | |_| | | (_| | |_| | (_) | | | | ___) |  __/ |   \ V /  __/ |   ");
    Logger.Write(LogType.NOTICE, @"|_____|_| |_| |_|\__,_|_|\__,_|\__|_|\___/|_| |_||____/ \___|_|    \_/ \___|_|   ");
    Logger.Write(LogType.NOTICE, @"                                                                                 ");
    Logger.Write(LogType.NOTICE, @"                          :: Realm Server ::");

    await using RealmServer server = new(settings);

    await server.StartAsync(cancellation.Token);
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    Logger.Write(LogType.INFORMATION, "Shutdown requested. Stopping RealmServer...", nameof(Program));
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
