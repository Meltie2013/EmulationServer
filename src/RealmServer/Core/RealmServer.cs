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

using EmulationServer.Database.Accounts;
using EmulationServer.Database.Interfaces;
using EmulationServer.Database.Services;
using EmulationServer.Network.Networking.Peers;
using EmulationServer.Network.Networking.Socket;
using EmulationServer.RealmServer.Auth;
using EmulationServer.RealmServer.Commands;
using EmulationServer.RealmServer.Configuration;
using EmulationServer.RealmServer.Internal;
using EmulationServer.RealmServer.Realms;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.RealmServer.Core;

public sealed class RealmServer : IAsyncDisposable
{
    private readonly RealmServerSettings _settings;
    private readonly IDatabaseService _databaseService;
    private readonly AccountRepository _accountRepository;
    private readonly ConfiguredRealmStore _realmStore;
    private readonly RealmSocketListener _socketListener;
    private readonly InternalSocketListener _internalSocketListener;
    private readonly InternalPeerConnector _internalPeerConnector;
    private readonly RealmConsoleCommandService _commandService;

    public RealmServer(RealmServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _settings = settings;
        _databaseService = new MySqlDatabaseService(settings.Database);
        _accountRepository = new AccountRepository(_databaseService);
        _realmStore = new ConfiguredRealmStore(settings.Realms);

        RealmListPacketBuilder realmListPacketBuilder = new(_realmStore);
        _socketListener = new RealmSocketListener(
            settings.Socket,
            () => new RealmAuthSessionProcessor(_accountRepository, realmListPacketBuilder));

        RealmInternalPacketHandler internalPacketHandler = new(_realmStore);
        _internalSocketListener = new InternalSocketListener(settings.InternalNetwork, internalPacketHandler.CreateCallbacks());
        _internalPeerConnector = new InternalPeerConnector(
            nameof(RealmServer),
            settings.InternalNetwork.Peers,
            settings.InternalNetwork.RegistrationKey,
            settings.InternalNetwork.LatencyReportInterval,
            settings.InternalNetwork.PingTimeout,
            internalPacketHandler.CreateCallbacks());

        _commandService = new RealmConsoleCommandService(_accountRepository);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.Write(LogType.NOTICE, "Starting RealmServer...", nameof(RealmServer));
        await ValidateStartupAsync(cancellationToken);

        _commandService.Start(cancellationToken);
        await _internalPeerConnector.StartAsync(cancellationToken);

        _ = Task.Run(() => _internalSocketListener.StartAsync(cancellationToken), CancellationToken.None);

        if (_settings.InternalNetwork.Peers.Count == 0)
        {
            Logger.Write(LogType.NETWORK, "RealmServer has no outgoing internal peers configured. Waiting for incoming realm status packets.", nameof(RealmServer));
        }

        Logger.Write(LogType.NETWORK, "RealmServer started successfully. Listening for authentication connections...", nameof(RealmServer));
        await _socketListener.StartAsync(cancellationToken);

        Logger.Write(LogType.TRACE, "RealmServer stopped.", nameof(RealmServer));
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _internalPeerConnector.StopAsync(cancellationToken);
        await _internalSocketListener.StopAsync(cancellationToken);
        await _socketListener.StopAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        await _internalPeerConnector.DisposeAsync();
        await _databaseService.DisposeAsync();
    }

    private async Task ValidateStartupAsync(CancellationToken cancellationToken)
    {
        Logger.Write(LogType.TRACE, "Validating RealmServer settings...", nameof(RealmServer));
        _settings.Validate();

        Logger.Write(LogType.TRACE, "Validating RealmServer critical authentication opcodes...", nameof(RealmServer));
        RealmAuthOpcodeVerifier.VerifyCriticalOpCodes();
        Logger.Write(LogType.TRACE, $"Validated RealmServer critical authentication opcodes...", nameof(RealmServer));

        Logger.Write(LogType.NETWORK, "Validating database connection...", nameof(RealmServer));
        await _databaseService.ValidateConnectionAsync(cancellationToken);

        Logger.Write(LogType.NETWORK, $"Loaded {_settings.Realms.Count} configured realm(s).", nameof(RealmServer));
        Logger.Write(LogType.NETWORK, "RealmServer settings, authentication opcodes, database connection, and internal networking validated successfully.", nameof(RealmServer));
    }
}
