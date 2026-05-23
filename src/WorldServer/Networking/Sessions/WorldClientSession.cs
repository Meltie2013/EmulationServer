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

using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.WorldServer.Auth;
using EmulationServer.WorldServer.Characters;
using EmulationServer.WorldServer.Database.Accounts;
using EmulationServer.WorldServer.Networking.Packets;

namespace EmulationServer.WorldServer.Networking.Sessions;

public sealed class WorldClientSession : IAsyncDisposable
{
    private readonly TcpClient _client;
    private readonly uint _realmId;
    private readonly int _maximumPacketSize;
    private readonly WorldAccountRepository _accountRepository;
    private readonly CharacterCreationService _characterService;
    private readonly Func<CancellationToken, Task> _characterCountChangedAsync;
    private readonly CancellationTokenSource _disconnect = new();
    private readonly uint _serverSeed;

    private NetworkStream? _stream;
    private WorldHeaderCrypt? _crypt;
    private WorldAccountSessionRecord? _account;
    private bool _disposed;

    public WorldClientSession(
        TcpClient client,
        uint realmId,
        int maximumPacketSize,
        WorldAccountRepository accountRepository,
        CharacterCreationService characterService,
        Func<CancellationToken, Task>? characterCountChangedAsync = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _realmId = realmId;
        _maximumPacketSize = maximumPacketSize;
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _characterService = characterService ?? throw new ArgumentNullException(nameof(characterService));
        _characterCountChangedAsync = characterCountChangedAsync ?? (_ => Task.CompletedTask);
        _serverSeed = unchecked((uint)RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue));
        Id = Guid.NewGuid();
    }

    public Guid Id { get; }

    public string RemoteEndPoint => _client.Client.RemoteEndPoint?.ToString() ?? "unknown";

    public async Task ProcessAsync(CancellationToken serverCancellationToken)
    {
        using CancellationTokenSource linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            serverCancellationToken,
            _disconnect.Token);

        CancellationToken cancellationToken = linkedCancellation.Token;
        _stream = _client.GetStream();

        try
        {
            await SendAsync(WorldOpcode.SMSG_AUTH_CHALLENGE, WorldPacketBuilders.BuildAuthChallenge(_serverSeed), null, cancellationToken);
            await AuthenticateAsync(cancellationToken);
            await ProcessAuthenticatedPacketsAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (EndOfStreamException)
        {
            Logger.Write(LogType.NETWORK, $"World client disconnected: {RemoteEndPoint}.", nameof(WorldClientSession));
        }
        catch (IOException exception)
        {
            Logger.Write(LogType.NETWORK, $"World client socket closed: {RemoteEndPoint}. {exception.Message}", nameof(WorldClientSession));
        }
        catch (SocketException exception)
        {
            Logger.Write(LogType.NETWORK, $"World client socket failed: {RemoteEndPoint}. {exception.Message}", nameof(WorldClientSession));
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.FAILED, $"World client session failed for {RemoteEndPoint}: {exception}", nameof(WorldClientSession));
        }
        finally
        {
            await DisconnectAsync();
        }
    }

    public async Task DisconnectAsync()
    {
        if (!_disconnect.IsCancellationRequested)
        {
            await _disconnect.CancelAsync();
        }

        try
        {
            _client.Close();
        }
        catch
        {
            // Ignore shutdown races.
        }
    }

    private async Task AuthenticateAsync(CancellationToken cancellationToken)
    {
        WorldPacket packet = await WorldPacketIO.ReadClientPacketAsync(GetStream(), null, _maximumPacketSize, cancellationToken);
        if (packet.Opcode != WorldOpcode.CMSG_AUTH_SESSION)
        {
            Logger.Write(LogType.WARNING, $"World client {RemoteEndPoint} sent {packet.Opcode} before CMSG_AUTH_SESSION.", nameof(WorldClientSession));
            await SendAsync(WorldOpcode.SMSG_AUTH_RESPONSE, WorldPacketBuilders.BuildAuthResponse(AuthResponseCode.Failed), null, cancellationToken);
            throw new InvalidDataException("World client did not send CMSG_AUTH_SESSION first.");
        }

        WorldAuthSessionRequest request = WorldAuthSessionParser.Parse(packet.Payload);
        string username = WorldAccountRepository.NormalizeUsername(request.Username);
        WorldAccountSessionRecord? account = await _accountRepository.GetAccountSessionAsync(username, cancellationToken);
        if (account is null || account.Locked)
        {
            Logger.Write(LogType.WARNING, $"World auth rejected for '{username}' from {RemoteEndPoint}: account missing or locked.", nameof(WorldClientSession));
            await SendAsync(WorldOpcode.SMSG_AUTH_RESPONSE, WorldPacketBuilders.BuildAuthResponse(AuthResponseCode.Failed), null, cancellationToken);
            throw new UnauthorizedAccessException("World account authentication failed.");
        }

        byte[] sessionKey = WorldAuthCryptography.ParseSessionKey(account.SessionKey);
        if (!WorldAuthCryptography.ProofMatches(username, request.ClientSeed, _serverSeed, sessionKey, request.ClientProof))
        {
            Logger.Write(LogType.WARNING, $"World auth proof failed for '{username}' from {RemoteEndPoint}.", nameof(WorldClientSession));
            await SendAsync(WorldOpcode.SMSG_AUTH_RESPONSE, WorldPacketBuilders.BuildAuthResponse(AuthResponseCode.Failed), null, cancellationToken);
            throw new UnauthorizedAccessException("World account proof failed.");
        }

        _account = account;
        _crypt = new WorldHeaderCrypt(sessionKey);
        await _accountRepository.SetActiveRealmAsync(account.Id, _realmId, cancellationToken);

        await SendAsync(WorldOpcode.SMSG_ADDON_INFO, WorldPacketBuilders.BuildAddonInfo(request.AddonInfo), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_AUTH_RESPONSE, WorldPacketBuilders.BuildAuthResponse(AuthResponseCode.Ok), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_ACCOUNT_DATA_TIMES, WorldPacketBuilders.BuildAccountDataTimes(), _crypt, cancellationToken);

        Logger.Write(LogType.SUCCESS, $"World client authenticated account '{account.Username}' ({account.Id}) from {RemoteEndPoint}.", nameof(WorldClientSession));
    }

    private async Task ProcessAuthenticatedPacketsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            WorldPacket packet = await WorldPacketIO.ReadClientPacketAsync(GetStream(), _crypt, _maximumPacketSize, cancellationToken);
            switch (packet.Opcode)
            {
                case WorldOpcode.CMSG_PING:
                    await HandlePingAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_CHAR_ENUM:
                    await HandleCharacterEnumAsync(cancellationToken);
                    break;

                case WorldOpcode.CMSG_CHAR_CREATE:
                    await HandleCharacterCreateAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_REQUEST_ACCOUNT_DATA:
                    await HandleRequestAccountDataAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_UPDATE_ACCOUNT_DATA:
                    Logger.Write(LogType.TRACE, $"Received CMSG_UPDATE_ACCOUNT_DATA from {RemoteEndPoint}; persistence is not implemented yet.", nameof(WorldClientSession));
                    break;

                case WorldOpcode.CMSG_CHAR_DELETE:
                    await HandleCharacterDeleteAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_PLAYER_LOGIN:
                    Logger.Write(LogType.INFORMATION, "CMSG_PLAYER_LOGIN received but world entry is intentionally not implemented in this milestone.", nameof(WorldClientSession));
                    break;

                default:
                    Logger.Write(LogType.TRACE, $"Unhandled world opcode from {RemoteEndPoint}: {packet.Opcode} (0x{(ushort)packet.Opcode:X4}), payload={packet.Payload.Length} byte(s).", nameof(WorldClientSession));
                    break;
            }
        }
    }

    private async Task HandlePingAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        WorldPacketReader reader = new(packet.Payload);
        uint sequence = reader.ReadUInt32();
        _ = packet.Payload.Length >= 8 ? reader.ReadUInt32() : 0;

        await SendAsync(WorldOpcode.SMSG_PONG, WorldPacketBuilders.BuildPong(sequence), _crypt, cancellationToken);
    }

    private async Task HandleCharacterEnumAsync(CancellationToken cancellationToken)
    {
        WorldAccountSessionRecord account = RequireAccount();

        try
        {
            IReadOnlyList<CharacterListEntry> characters = await _characterService.GetCharacterListAsync(account.Id, cancellationToken);
            byte[] payload = WorldPacketBuilders.BuildCharacterEnum(characters);
            await SendAsync(WorldOpcode.SMSG_CHAR_ENUM, payload, _crypt, cancellationToken);

            Logger.Write(LogType.NETWORK, $"Sent character list to account '{account.Username}': {characters.Count} character(s), payload={payload.Length} byte(s).", nameof(WorldClientSession));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Logger.Write(LogType.FAILED, $"Failed to build/send character list for account '{account.Username}' ({account.Id}): {exception}", nameof(WorldClientSession));
            await SendAsync(WorldOpcode.SMSG_CHAR_ENUM, WorldPacketBuilders.BuildCharacterEnum(Array.Empty<CharacterListEntry>()), _crypt, cancellationToken);
        }
    }

    private async Task HandleRequestAccountDataAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        uint accountDataType = 0;
        if (packet.Payload.Length >= sizeof(uint))
        {
            WorldPacketReader reader = new(packet.Payload);
            accountDataType = reader.ReadUInt32();
        }

        await SendAsync(WorldOpcode.SMSG_UPDATE_ACCOUNT_DATA, WorldPacketBuilders.BuildUpdateAccountData(accountDataType), _crypt, cancellationToken);
    }

    private async Task HandleCharacterCreateAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        WorldAccountSessionRecord account = RequireAccount();
        CharacterCreateRequest request = ReadCharacterCreateRequest(packet.Payload);
        CharacterCreateResult result = await _characterService.CreateCharacterAsync(account.Id, request, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_CHAR_CREATE, WorldPacketBuilders.BuildCharacterCreate(result), _crypt, cancellationToken);

        if (result == CharacterCreateResult.Success)
        {
            await _characterCountChangedAsync(cancellationToken);
        }

        Logger.Write(
            result == CharacterCreateResult.Success ? LogType.SUCCESS : LogType.WARNING,
            $"Character create result for account '{account.Username}', name='{request.Name}': {result}.",
            nameof(WorldClientSession));
    }

    private async Task HandleCharacterDeleteAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        WorldAccountSessionRecord account = RequireAccount();
        ulong clientGuid = ReadCharacterDeleteGuid(packet.Payload);
        CharacterDeleteServiceResult result = await _characterService.DeleteCharacterAsync(account.Id, clientGuid, cancellationToken);

        if (result == CharacterDeleteServiceResult.SecurityMismatch)
        {
            // Match MaNGOS' defensive behavior: a crafted request for a character
            // not owned by this authenticated account is ignored after logging.
            return;
        }

        CharacterDeleteResult clientResult = result == CharacterDeleteServiceResult.Success
            ? CharacterDeleteResult.Success
            : CharacterDeleteResult.Failed;

        await SendAsync(WorldOpcode.SMSG_CHAR_DELETE, WorldPacketBuilders.BuildCharacterDelete(clientResult), _crypt, cancellationToken);

        if (result == CharacterDeleteServiceResult.Success)
        {
            await _characterCountChangedAsync(cancellationToken);
        }

        Logger.Write(
            result == CharacterDeleteServiceResult.Success ? LogType.SUCCESS : LogType.WARNING,
            $"Character delete result for account '{account.Username}', guid=0x{clientGuid:X16}: {result}.",
            nameof(WorldClientSession));
    }

    private static ulong ReadCharacterDeleteGuid(byte[] payload)
    {
        WorldPacketReader reader = new(payload);
        return reader.ReadUInt64();
    }

    private static CharacterCreateRequest ReadCharacterCreateRequest(byte[] payload)
    {
        WorldPacketReader reader = new(payload);
        string name = reader.ReadCString();
        return new CharacterCreateRequest(
            name,
            reader.ReadUInt8(),
            reader.ReadUInt8(),
            reader.ReadUInt8(),
            reader.ReadUInt8(),
            reader.ReadUInt8(),
            reader.ReadUInt8(),
            reader.ReadUInt8(),
            reader.ReadUInt8(),
            reader.ReadUInt8());
    }

    private async Task SendAsync(WorldOpcode opcode, byte[] payload, WorldHeaderCrypt? crypt, CancellationToken cancellationToken)
    {
        await WorldPacketIO.WriteServerPacketAsync(GetStream(), opcode, payload, crypt, cancellationToken);
    }

    private NetworkStream GetStream()
    {
        return _stream ?? throw new InvalidOperationException("World client stream is not initialized.");
    }

    private WorldAccountSessionRecord RequireAccount()
    {
        return _account ?? throw new InvalidOperationException("World client is not authenticated.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await DisconnectAsync();
        _disconnect.Dispose();
        _client.Dispose();
    }
}
