using EmulationServer.Core.Servers;
using EmulationServer.Game.Data.Stores;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Peers;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.WorldServer.Configuration;
using EmulationServer.WorldServer.Internal;

namespace EmulationServer.WorldServer.Core;

public sealed class WorldServer : IAsyncDisposable
{
    private readonly WorldServerSettings _settings;
    private readonly EmulationServerHost _host;
    private readonly WorldRealmStatusReporter _realmStatusReporter;

    private WorldGameDataStore _gameData = WorldGameDataStore.Empty;

    public WorldServer(WorldServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _settings = settings;
        _host = new EmulationServerHost(nameof(WorldServer), settings.Database, settings.InternalNetwork, CreateCallbacks());
        _realmStatusReporter = new WorldRealmStatusReporter(
            settings.RealmStatus,
            settings.InternalNetwork.RegistrationKey,
            settings.MaxConnections,
            settings.InternalNetwork.LatencyReportInterval,
            settings.InternalNetwork.PingTimeout);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LoadGameDataIfEnabled();

        Task hostTask = _host.StartAsync(cancellationToken);

        try
        {
            await _host.StartupCompleted.WaitAsync(cancellationToken);

            await _realmStatusReporter.StartAsync(cancellationToken);

            await hostTask;
        }
        finally
        {
            await _realmStatusReporter.StopAsync(CancellationToken.None);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _realmStatusReporter.StopAsync(cancellationToken);
        await _host.StopAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        await _realmStatusReporter.DisposeAsync();
        await _host.DisposeAsync();
    }

    private InternalNetworkCallbacks CreateCallbacks()
    {
        return new InternalNetworkCallbacks
        {
            PeerAuthenticatedAsync = OnPeerAuthenticatedAsync,
        };
    }

    private async Task OnPeerAuthenticatedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(remoteServerName, "ProxyServer", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string packet = $"{InternalProtocol.WorldCapacity} {_settings.MaxConnections}";
        await connection.SendPacketAsync(packet, cancellationToken);

        Logger.Write(LogType.NETWORK, $"WorldServer announced max connections to ProxyServer: {_settings.MaxConnections}.", nameof(WorldServer));
    }

    private void LoadGameDataIfEnabled()
    {
        GameDataSettings gameDataSettings = _settings.GameData;
        if (!gameDataSettings.Enabled)
        {
            Logger.Write(LogType.INFORMATION, "WorldServer game data loading is disabled. Enable [GameData] when extracted DBC/maps are ready.", nameof(WorldServer));
            return;
        }

        Logger.Write(LogType.NOTICE, "WorldServer loading DBC and map data into memory...");

        _gameData = WorldGameDataStore.Load(
            gameDataSettings.DataDirectory,
            gameDataSettings.DbcDirectory,
            gameDataSettings.MapsDirectory,
            gameDataSettings.RequiredDbcFiles,
            gameDataSettings.LoadMaps);

        Logger.Write(LogType.SUCCESS, $"WorldServer game data is ready in memory: {_gameData.DbcStores.Count} DBC store(s), {_gameData.MapTiles.Count} map tile(s).", nameof(WorldServer));
    }
}
