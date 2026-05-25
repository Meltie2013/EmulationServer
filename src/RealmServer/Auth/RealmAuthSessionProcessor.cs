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

using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

using EmulationServer.Database.Accounts;
using EmulationServer.Network.Networking.Sessions;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/RealmServer/Auth/RealmAuthSessionProcessor.cs
  * Documents the RealmAuthSessionProcessor source file in the realm authentication, realm-list handling, and external client login services area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.RealmServer.Auth;

/**
  * Owns the realm auth session processor behavior for the realm authentication, realm-list handling, and external client login services layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class RealmAuthSessionProcessor : IRealmSessionProcessor
{
    /**
      * Defines the short grace window used after terminal auth failures so the vanilla client can render the exact failure text before the socket closes.
      */
    private static readonly TimeSpan TerminalAuthFailureDeliveryDelay = TimeSpan.FromMilliseconds(250);

    /**
      * Holds the private account repository state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly AccountRepository _accountRepository;
    /**
      * Holds the private realm list packet builder state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly RealmListPacketBuilder _realmListPacketBuilder;

    /**
      * Holds the private status state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private RealmAuthStatus _status = RealmAuthStatus.Challenge;
    /**
      * Holds the private account state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private AccountLogonRecord? _account;
    /**
      * Holds the private login state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private string _login = string.Empty;
    /**
      * Holds the private os state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private string _os = string.Empty;
    /**
      * Holds the private locale name state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private string _localeName = "enUS";
    /**
      * Holds the private locale state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private byte _locale;
    /**
      * Holds the private build state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private ushort _build;
    /**
      * Holds the private salt state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private BigInteger _salt;
    /**
      * Holds the private verifier state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private BigInteger _verifier;
    /**
      * Holds the private host private ephemeral state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private BigInteger _hostPrivateEphemeral;
    /**
      * Holds the private host public ephemeral state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private BigInteger _hostPublicEphemeral;
    /**
      * Holds the private session key state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private byte[] _sessionKey = [];
    /**
      * Holds the reconnect challenge bytes sent during CMD_AUTH_RECONNECT_CHALLENGE.
      * The follow-up proof must hash these same bytes so the client can renew the realm session after a world rejection.
      */
    private byte[] _reconnectChallenge = [];
    /**
      * Holds the reconnect checksum salt bytes sent with the reconnect challenge.
      * Vanilla uses a 16-byte salt field even when the server does not enforce a patch checksum.
      */
    private byte[] _reconnectChecksumSalt = [];

    /**
      * Initializes a new RealmAuthSessionProcessor instance with the dependencies required by the realm authentication, realm-list handling, and external client login services workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: accountRepository, realmListPacketBuilder.
      */
    public RealmAuthSessionProcessor(AccountRepository accountRepository, RealmListPacketBuilder realmListPacketBuilder)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException();
        _realmListPacketBuilder = realmListPacketBuilder ?? throw new ArgumentNullException();
    }

    /**
      * Processes incoming data and dispatches it to the correct subsystem handler.
      * The method is part of RealmAuthSessionProcessor and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task ProcessAsync(RealmSessionContext context, CancellationToken cancellationToken)
    {
        Logger.Write(LogType.NETWORK, $"Realm auth session started for {context.RemoteEndPoint}.", "RealmAuthSessionProcessor");

        while (!cancellationToken.IsCancellationRequested && _status != RealmAuthStatus.Closed)
        {
            byte command = await context.ReadByteAsync(cancellationToken);

            switch ((RealmAuthOpCode)command)
            {
                case RealmAuthOpCode.AuthLogonChallenge when _status == RealmAuthStatus.Challenge:
                    await HandleLogonChallengeAsync(context, cancellationToken);
                    break;

                case RealmAuthOpCode.AuthLogonProof when _status == RealmAuthStatus.LogonProof:
                    await HandleLogonProofAsync(context, cancellationToken);
                    break;

                case RealmAuthOpCode.AuthReconnectChallenge when _status == RealmAuthStatus.Challenge:
                    await HandleReconnectChallengeAsync(context, cancellationToken);
                    break;

                case RealmAuthOpCode.AuthReconnectProof when _status == RealmAuthStatus.ReconnectProof:
                    await HandleReconnectProofAsync(context, cancellationToken);
                    break;

                case RealmAuthOpCode.RealmList when _status == RealmAuthStatus.Authenticated:
                    await HandleRealmListAsync(context, cancellationToken);
                    break;

                default:
                    Logger.Write(LogType.WARNING, $"Received unauthorized RealmServer auth command 0x{command:X2} from {context.RemoteEndPoint} while status is {_status}.", "RealmAuthSessionProcessor");
                    _status = RealmAuthStatus.Closed;
                    break;
            }
        }
    }

    /**
      * Handles a single operation or packet and keeps the calling code focused on flow control.
      * The method is part of RealmAuthSessionProcessor and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task HandleLogonChallengeAsync(RealmSessionContext context, CancellationToken cancellationToken)
    {
        byte protocolVersion = await context.ReadByteAsync(cancellationToken);
        byte[] sizeBytes = await context.ReadBytesAsync(2, cancellationToken);
        ushort remaining = BinaryPrimitives.ReadUInt16LittleEndian(sizeBytes);

        if (remaining < 30)
        {
            Logger.Write(LogType.WARNING, $"Invalid logon challenge size '{remaining}' from {context.RemoteEndPoint}.", "RealmAuthSessionProcessor");
            _status = RealmAuthStatus.Closed;
            return;
        }

        Logger.Write(LogType.TRACE, $"Received auth protocol version 0x{protocolVersion:X2} with logon challenge size {remaining} from {context.RemoteEndPoint}.", "RealmAuthSessionProcessor");

        byte[] payload = await context.ReadBytesAsync(remaining, cancellationToken);
        if (!TryParseLogonChallenge(payload, out LogonChallenge challenge))
        {
            await SendChallengeFailureAndCloseAsync(context, RealmAuthResult.Failed, cancellationToken);
            return;
        }

        _login = AccountRepository.NormalizeUsername(challenge.Username);
        _build = challenge.Build;
        _os = challenge.OperatingSystem;
        _localeName = challenge.LocaleName;
        _locale = GetLocaleIndex(_localeName);

        Logger.Write(LogType.NETWORK, $"Received logon challenge for account '{_login}' using client build {_build} from {context.RemoteEndPoint}.", "RealmAuthSessionProcessor");

        if (!RealmBuilds.IsSupported(_build))
        {
            await SendChallengeFailureAndCloseAsync(context, RealmAuthResult.VersionInvalid, cancellationToken);
            return;
        }

        if (await _accountRepository.IsIpBannedAsync(context.RemoteAddress, cancellationToken))
        {
            Logger.Write(LogType.WARNING, $"Banned IP '{context.RemoteAddress}' attempted to authenticate.", "RealmAuthSessionProcessor");
            await SendChallengeFailureAndCloseAsync(context, RealmAuthResult.Banned, cancellationToken);
            return;
        }

        _account = await _accountRepository.GetForLogonAsync(_login, cancellationToken);
        if (_account is null)
        {
            Logger.Write(LogType.WARNING, $"Unknown account '{_login}' attempted to authenticate.", "RealmAuthSessionProcessor");
            await SendChallengeFailureAndCloseAsync(context, RealmAuthResult.UnknownAccount, cancellationToken);
            return;
        }

        if (_account.Locked && !string.Equals(_account.LastIp, context.RemoteAddress, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.WARNING, $"Locked account '{_login}' attempted to login from invalid IP '{context.RemoteAddress}'.", "RealmAuthSessionProcessor");
            await SendChallengeFailureAndCloseAsync(context, RealmAuthResult.LockedEnforced, cancellationToken);
            return;
        }

        AccountBanStatus banStatus = await _accountRepository.GetAccountBanStatusAsync(_account.Id, cancellationToken);
        if (banStatus.IsBanned)
        {
            Logger.Write(LogType.WARNING, $"Banned account '{_login}' attempted to authenticate.", "RealmAuthSessionProcessor");
            await SendChallengeFailureAndCloseAsync(context, banStatus.IsPermanent ? RealmAuthResult.Banned : RealmAuthResult.Suspended, cancellationToken);
            return;
        }

        await PrepareSrpChallengeAsync(_account, cancellationToken);
        await SendChallengeSuccessAsync(context, cancellationToken);

        _status = RealmAuthStatus.LogonProof;
    }

    /**
      * Handles CMD_AUTH_RECONNECT_CHALLENGE so clients returning from a rejected or closed world session can renew their realm-list session.
      * The reconnect challenge shares the same client packet body as the normal logon challenge, but it uses the stored session key instead of a password proof.
      */
    private async Task HandleReconnectChallengeAsync(RealmSessionContext context, CancellationToken cancellationToken)
    {
        byte protocolVersion = await context.ReadByteAsync(cancellationToken);
        byte[] sizeBytes = await context.ReadBytesAsync(2, cancellationToken);
        ushort remaining = BinaryPrimitives.ReadUInt16LittleEndian(sizeBytes);

        if (remaining < 30)
        {
            Logger.Write(LogType.WARNING, $"Invalid reconnect challenge size '{remaining}' from {context.RemoteEndPoint}.", "RealmAuthSessionProcessor");
            await SendReconnectChallengeFailureAndCloseAsync(context, RealmAuthResult.Failed, cancellationToken);
            return;
        }

        Logger.Write(LogType.TRACE, $"Received auth protocol version 0x{protocolVersion:X2} with reconnect challenge size {remaining} from {context.RemoteEndPoint}.", "RealmAuthSessionProcessor");

        byte[] payload = await context.ReadBytesAsync(remaining, cancellationToken);
        if (!TryParseLogonChallenge(payload, out LogonChallenge challenge))
        {
            await SendReconnectChallengeFailureAndCloseAsync(context, RealmAuthResult.Failed, cancellationToken);
            return;
        }

        _login = AccountRepository.NormalizeUsername(challenge.Username);
        _build = challenge.Build;
        _os = challenge.OperatingSystem;
        _localeName = challenge.LocaleName;
        _locale = GetLocaleIndex(_localeName);

        Logger.Write(LogType.NETWORK, $"Received reconnect challenge for account '{_login}' using client build {_build} from {context.RemoteEndPoint}.", "RealmAuthSessionProcessor");

        if (!RealmBuilds.IsSupported(_build))
        {
            await SendReconnectChallengeFailureAndCloseAsync(context, RealmAuthResult.VersionInvalid, cancellationToken);
            return;
        }

        if (await _accountRepository.IsIpBannedAsync(context.RemoteAddress, cancellationToken))
        {
            Logger.Write(LogType.WARNING, $"Banned IP '{context.RemoteAddress}' attempted to reconnect.", "RealmAuthSessionProcessor");
            await SendReconnectChallengeFailureAndCloseAsync(context, RealmAuthResult.Banned, cancellationToken);
            return;
        }

        _account = await _accountRepository.GetForLogonAsync(_login, cancellationToken);
        if (_account is null)
        {
            Logger.Write(LogType.WARNING, $"Unknown account '{_login}' attempted to reconnect.", "RealmAuthSessionProcessor");
            await SendReconnectChallengeFailureAndCloseAsync(context, RealmAuthResult.UnknownAccount, cancellationToken);
            return;
        }

        if (_account.Locked && !string.Equals(_account.LastIp, context.RemoteAddress, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.WARNING, $"Locked account '{_login}' attempted to reconnect from invalid IP '{context.RemoteAddress}'.", "RealmAuthSessionProcessor");
            await SendReconnectChallengeFailureAndCloseAsync(context, RealmAuthResult.LockedEnforced, cancellationToken);
            return;
        }

        AccountBanStatus banStatus = await _accountRepository.GetAccountBanStatusAsync(_account.Id, cancellationToken);
        if (banStatus.IsBanned)
        {
            Logger.Write(LogType.WARNING, $"Banned account '{_login}' attempted to reconnect.", "RealmAuthSessionProcessor");
            await SendReconnectChallengeFailureAndCloseAsync(context, banStatus.IsPermanent ? RealmAuthResult.Banned : RealmAuthResult.Suspended, cancellationToken);
            return;
        }

        if (!TryParseSessionKey(_account.SessionKey, out _sessionKey))
        {
            Logger.Write(LogType.WARNING, $"Account '{_login}' attempted reconnect but no valid stored session key was available.", "RealmAuthSessionProcessor");
            await SendReconnectChallengeFailureAndCloseAsync(context, RealmAuthResult.Failed, cancellationToken);
            return;
        }

        _reconnectChallenge = Srp6Utilities.GenerateRandomBytes(16);
        _reconnectChecksumSalt = new byte[16];

        await SendReconnectChallengeSuccessAsync(context, cancellationToken);
        _status = RealmAuthStatus.ReconnectProof;
    }

    /**
      * Handles a single operation or packet and keeps the calling code focused on flow control.
      * The method is part of RealmAuthSessionProcessor and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task HandleLogonProofAsync(RealmSessionContext context, CancellationToken cancellationToken)
    {
        if (_account is null)
        {
            _status = RealmAuthStatus.Closed;
            return;
        }

        byte[] proofPacket = await context.ReadBytesAsync(74, cancellationToken);

        BigInteger clientPublicEphemeral = Srp6Utilities.FromLittleEndian(proofPacket.AsSpan(0, Srp6Utilities.PublicKeyLength));
        byte[] clientProof = proofPacket.AsSpan(32, Srp6Utilities.ProofLength).ToArray();

        if (clientPublicEphemeral.IsZero || clientPublicEphemeral % Srp6Utilities.N == BigInteger.Zero)
        {
            Logger.Write(LogType.WARNING, $"Account '{_login}' sent invalid SRP6 client public ephemeral.", "RealmAuthSessionProcessor");
            await SendProofFailureAndCloseAsync(context, cancellationToken);
            return;
        }

        BigInteger scrambler = Srp6Utilities.CalculateScrambler(clientPublicEphemeral, _hostPublicEphemeral);
        BigInteger sessionSecret = Srp6Utilities.CalculateSessionSecret(clientPublicEphemeral, _verifier, scrambler, _hostPrivateEphemeral);
        _sessionKey = Srp6Utilities.HashSessionKey(sessionSecret);

        byte[] expectedProof = Srp6Utilities.CalculateClientProof(_login, _salt, clientPublicEphemeral, _hostPublicEphemeral, _sessionKey);

        if (!Srp6Utilities.FixedTimeEquals(expectedProof, clientProof))
        {
            Logger.Write(LogType.WARNING, $"Account '{_login}' failed SRP6 proof validation.", "RealmAuthSessionProcessor");
            await _accountRepository.IncrementFailedLoginsAsync(_login, cancellationToken);
            await SendProofFailureAndCloseAsync(context, cancellationToken);
            return;
        }

        string sessionKeyHex = Convert.ToHexString(_sessionKey).ToLowerInvariant();
        await _accountRepository.UpdateSuccessfulLoginAsync(_login, sessionKeyHex, context.RemoteAddress, _locale, _os, cancellationToken);

        byte[] hostProof = Srp6Utilities.CalculateHostProof(clientPublicEphemeral, clientProof, _sessionKey);
        await SendProofSuccessAsync(context, hostProof, cancellationToken);

        Logger.Write(LogType.SUCCESS, $"Account '{_login}' authenticated successfully from {context.RemoteEndPoint}.", "RealmAuthSessionProcessor");

        _status = RealmAuthStatus.Authenticated;
    }

    /**
      * Handles CMD_AUTH_RECONNECT_PROOF and promotes the session back to the authenticated realm-list state when the stored session key matches.
      */
    private async Task HandleReconnectProofAsync(RealmSessionContext context, CancellationToken cancellationToken)
    {
        if (_account is null || _sessionKey.Length == 0 || _reconnectChallenge.Length == 0)
        {
            await SendReconnectProofFailureAndCloseAsync(context, cancellationToken);
            return;
        }

        byte[] proofPacket = await context.ReadBytesAsync(57, cancellationToken);
        byte[] proofData = proofPacket.AsSpan(0, 16).ToArray();
        byte[] clientProof = proofPacket.AsSpan(16, Srp6Utilities.ProofLength).ToArray();

        byte[] expectedProof = CalculateReconnectProof(proofData);
        if (!Srp6Utilities.FixedTimeEquals(expectedProof, clientProof))
        {
            Logger.Write(LogType.WARNING, $"Account '{_login}' failed reconnect proof validation.", "RealmAuthSessionProcessor");
            await SendReconnectProofFailureAndCloseAsync(context, cancellationToken);
            return;
        }

        string sessionKeyHex = Convert.ToHexString(_sessionKey).ToLowerInvariant();
        await _accountRepository.UpdateSuccessfulLoginAsync(_login, sessionKeyHex, context.RemoteAddress, _locale, _os, cancellationToken);
        await SendReconnectProofSuccessAsync(context, cancellationToken);

        Logger.Write(LogType.SUCCESS, $"Account '{_login}' reconnected successfully from {context.RemoteEndPoint}.", "RealmAuthSessionProcessor");
        _status = RealmAuthStatus.Authenticated;
    }

    /**
      * Handles a single operation or packet and keeps the calling code focused on flow control.
      * The method is part of RealmAuthSessionProcessor and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task HandleRealmListAsync(RealmSessionContext context, CancellationToken cancellationToken)
    {
        _ = await context.ReadBytesAsync(4, cancellationToken);

        if (_account is null)
        {
            _status = RealmAuthStatus.Closed;
            return;
        }

        byte[] packet = await _realmListPacketBuilder.BuildRealmListAsync(_build, (byte)_account.SecurityLevel, _account.Id, cancellationToken);
        await context.WriteAsync(packet, cancellationToken);

        Logger.Write(LogType.TRACE, $"Sent realm list to account '{_login}'.", "RealmAuthSessionProcessor");
    }

    /**
      * Performs the prepare srp challenge operation for the realm authentication, realm-list handling, and external client login services workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: account, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task PrepareSrpChallengeAsync(AccountLogonRecord account, CancellationToken cancellationToken)
    {
        if (Srp6Utilities.IsValidStoredSrpValue(account.Verifier) && Srp6Utilities.IsValidStoredSrpValue(account.Salt))
        {
            _verifier = Srp6Utilities.FromBigEndianHex(account.Verifier!);
            _salt = Srp6Utilities.FromBigEndianHex(account.Salt!);
        }
        else
        {
            _salt = Srp6Utilities.GenerateSalt();
            _verifier = Srp6Utilities.CalculateVerifier(_salt, account.ShaPassHash);

            string verifierHex = Srp6Utilities.ToBigEndianHex(_verifier, Srp6Utilities.SaltLength);
            string saltHex = Srp6Utilities.ToBigEndianHex(_salt, Srp6Utilities.SaltLength);

            await _accountRepository.UpdateVerifierAsync(account.Username, verifierHex, saltHex, cancellationToken);
        }

        _hostPrivateEphemeral = Srp6Utilities.GeneratePrivateEphemeral();
        _hostPublicEphemeral = Srp6Utilities.CalculateHostPublicEphemeral(_verifier, _hostPrivateEphemeral);
    }

    /**
      * Sends a protocol message or status update to a connected peer.
      * The method is part of RealmAuthSessionProcessor and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task SendChallengeFailureAsync(RealmSessionContext context, RealmAuthResult result, CancellationToken cancellationToken)
    {
        ByteWriter packet = new();
        packet.WriteUInt8((byte)RealmAuthOpCode.AuthLogonChallenge);
        packet.WriteUInt8(0);
        packet.WriteUInt8((byte)result);

        await context.WriteAsync(packet.ToArray(), cancellationToken);
    }

    /**
      * Sends a reconnect challenge failure using the reconnect opcode layout expected by the auth client.
      */
    private async Task SendReconnectChallengeFailureAsync(RealmSessionContext context, RealmAuthResult result, CancellationToken cancellationToken)
    {
        ByteWriter packet = new();
        packet.WriteUInt8((byte)RealmAuthOpCode.AuthReconnectChallenge);
        packet.WriteUInt8((byte)result);

        await context.WriteAsync(packet.ToArray(), cancellationToken);
    }

    /**
      * Sends a reconnect challenge success containing the 16-byte proof challenge and 16-byte checksum salt.
      */
    private async Task SendReconnectChallengeSuccessAsync(RealmSessionContext context, CancellationToken cancellationToken)
    {
        ByteWriter packet = new();
        packet.WriteUInt8((byte)RealmAuthOpCode.AuthReconnectChallenge);
        packet.WriteUInt8((byte)RealmAuthResult.Success);
        packet.WriteBytes(_reconnectChallenge);
        packet.WriteBytes(_reconnectChecksumSalt);

        await context.WriteAsync(packet.ToArray(), cancellationToken);
    }

    /**
      * Sends a terminal challenge failure and keeps the socket alive long enough for the client to consume it.
      */
    private async Task SendChallengeFailureAndCloseAsync(RealmSessionContext context, RealmAuthResult result, CancellationToken cancellationToken)
    {
        await SendChallengeFailureAsync(context, result, cancellationToken);
        await RealmSessionContext.AllowTerminalResponseDeliveryAsync(TerminalAuthFailureDeliveryDelay, cancellationToken);
        _status = RealmAuthStatus.Closed;
    }

    /**
      * Sends a terminal reconnect challenge failure and keeps the socket alive long enough for the client to consume it.
      */
    private async Task SendReconnectChallengeFailureAndCloseAsync(RealmSessionContext context, RealmAuthResult result, CancellationToken cancellationToken)
    {
        await SendReconnectChallengeFailureAsync(context, result, cancellationToken);
        await RealmSessionContext.AllowTerminalResponseDeliveryAsync(TerminalAuthFailureDeliveryDelay, cancellationToken);
        _status = RealmAuthStatus.Closed;
    }

    /**
      * Sends a terminal proof failure and keeps the socket alive long enough for the client to consume it.
      */
    private async Task SendProofFailureAndCloseAsync(RealmSessionContext context, CancellationToken cancellationToken)
    {
        await SendProofFailureAsync(context, cancellationToken);
        await RealmSessionContext.AllowTerminalResponseDeliveryAsync(TerminalAuthFailureDeliveryDelay, cancellationToken);
        _status = RealmAuthStatus.Closed;
    }

    /**
      * Sends a protocol message or status update to a connected peer.
      * The method is part of RealmAuthSessionProcessor and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task SendChallengeSuccessAsync(RealmSessionContext context, CancellationToken cancellationToken)
    {
        ByteWriter packet = new();
        packet.WriteUInt8((byte)RealmAuthOpCode.AuthLogonChallenge);
        packet.WriteUInt8(0);
        packet.WriteUInt8((byte)RealmAuthResult.Success);
        packet.WriteBytes(Srp6Utilities.ToLittleEndian(_hostPublicEphemeral, Srp6Utilities.PublicKeyLength));
        packet.WriteUInt8(1);
        packet.WriteBytes(Srp6Utilities.ToLittleEndian(Srp6Utilities.G));
        packet.WriteUInt8(Srp6Utilities.PublicKeyLength);
        packet.WriteBytes(Srp6Utilities.ToLittleEndian(Srp6Utilities.N, Srp6Utilities.PublicKeyLength));
        packet.WriteBytes(Srp6Utilities.ToLittleEndian(_salt, Srp6Utilities.SaltLength));
        packet.WriteBytes(Srp6Utilities.GenerateRandomBytes(16));
        packet.WriteUInt8(0);

        await context.WriteAsync(packet.ToArray(), cancellationToken);
    }

    /**
      * Sends a protocol message or status update to a connected peer.
      * The method is part of RealmAuthSessionProcessor and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task SendProofSuccessAsync(RealmSessionContext context, byte[] hostProof, CancellationToken cancellationToken)
    {
        ByteWriter packet = new();
        packet.WriteUInt8((byte)RealmAuthOpCode.AuthLogonProof);
        packet.WriteUInt8((byte)RealmAuthResult.Success);
        packet.WriteBytes(hostProof);

        if (RealmBuilds.UsesModernProofResponse(_build))
        {
            packet.WriteUInt32(0x00800000);
            packet.WriteUInt32(0x00000000);
            packet.WriteUInt16(0x0000);
        }
        else
        {
            packet.WriteUInt32(0x00000000);
        }

        await context.WriteAsync(packet.ToArray(), cancellationToken);
    }

    /**
      * Sends a protocol message or status update to a connected peer.
      * The method is part of RealmAuthSessionProcessor and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task SendProofFailureAsync(RealmSessionContext context, CancellationToken cancellationToken)
    {
        ByteWriter packet = new();
        packet.WriteUInt8((byte)RealmAuthOpCode.AuthLogonProof);
        packet.WriteUInt8((byte)RealmAuthResult.UnknownAccount);

        if (_build > RealmBuilds.Vanilla1122)
        {
            packet.WriteUInt8(3);
            packet.WriteUInt8(0);
        }

        await context.WriteAsync(packet.ToArray(), cancellationToken);
    }

    /**
      * Sends a successful reconnect proof response and includes the vanilla-era padding bytes only for builds that expect them.
      */
    private async Task SendReconnectProofSuccessAsync(RealmSessionContext context, CancellationToken cancellationToken)
    {
        ByteWriter packet = new();
        packet.WriteUInt8((byte)RealmAuthOpCode.AuthReconnectProof);
        packet.WriteUInt8((byte)RealmAuthResult.Success);

        if (_build > RealmBuilds.Vanilla1123)
        {
            packet.WriteUInt16(0);
        }

        await context.WriteAsync(packet.ToArray(), cancellationToken);
    }

    /**
      * Sends a reconnect proof failure response before closing so the client sees a protocol reason instead of a dropped socket.
      */
    private async Task SendReconnectProofFailureAsync(RealmSessionContext context, CancellationToken cancellationToken)
    {
        ByteWriter packet = new();
        packet.WriteUInt8((byte)RealmAuthOpCode.AuthReconnectProof);
        packet.WriteUInt8((byte)RealmAuthResult.Failed);

        if (_build > RealmBuilds.Vanilla1123)
        {
            packet.WriteUInt16(0);
        }

        await context.WriteAsync(packet.ToArray(), cancellationToken);
    }

    /**
      * Sends a terminal reconnect proof failure and keeps the socket alive long enough for the client to consume it.
      */
    private async Task SendReconnectProofFailureAndCloseAsync(RealmSessionContext context, CancellationToken cancellationToken)
    {
        await SendReconnectProofFailureAsync(context, cancellationToken);
        await RealmSessionContext.AllowTerminalResponseDeliveryAsync(TerminalAuthFailureDeliveryDelay, cancellationToken);
        _status = RealmAuthStatus.Closed;
    }

    /**
      * Calculates the reconnect proof hash from the account name, client proof data, server reconnect challenge, and stored session key.
      */
    private byte[] CalculateReconnectProof(byte[] proofData)
    {
        byte[] loginBytes = Encoding.UTF8.GetBytes(_login);
        byte[] proofInput = new byte[loginBytes.Length + proofData.Length + _reconnectChallenge.Length + _sessionKey.Length];
        int offset = 0;

        Buffer.BlockCopy(loginBytes, 0, proofInput, offset, loginBytes.Length);
        offset += loginBytes.Length;
        Buffer.BlockCopy(proofData, 0, proofInput, offset, proofData.Length);
        offset += proofData.Length;
        Buffer.BlockCopy(_reconnectChallenge, 0, proofInput, offset, _reconnectChallenge.Length);
        offset += _reconnectChallenge.Length;
        Buffer.BlockCopy(_sessionKey, 0, proofInput, offset, _sessionKey.Length);

        return SHA1.HashData(proofInput);
    }

    /**
      * Validates and parses the stored 40-byte SRP session key used by reconnect authentication.
      */
    private static bool TryParseSessionKey(string? sessionKeyHex, out byte[] sessionKey)
    {
        sessionKey = [];
        if (string.IsNullOrWhiteSpace(sessionKeyHex))
        {
            return false;
        }

        string normalized = sessionKeyHex.Trim();
        if (normalized.Length != Srp6Utilities.SessionKeyLength * 2 || !normalized.All(Uri.IsHexDigit))
        {
            return false;
        }

        sessionKey = Convert.FromHexString(normalized);
        return sessionKey.Length == Srp6Utilities.SessionKeyLength;
    }

    /**
      * Attempts the operation without treating a normal failure as an exceptional condition.
      * The method is part of RealmAuthSessionProcessor and keeps this workflow isolated from the caller.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    private static bool TryParseLogonChallenge(byte[] payload, out LogonChallenge challenge)
    {
        challenge = default;

        if (payload.Length < 30)
        {
            return false;
        }

        ushort build = BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(7, 2));
        string operatingSystem = ReverseFourCharacterString(payload.AsSpan(13, 4));
        string localeName = ReverseFourCharacterString(payload.AsSpan(17, 4));
        byte usernameLength = payload[29];

        if (payload.Length < 30 + usernameLength)
        {
            return false;
        }

        string username = Encoding.UTF8.GetString(payload, 30, usernameLength);
        if (string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        challenge = new LogonChallenge(build, operatingSystem, localeName, username);
        return true;
    }

    /**
      * Performs the reverse four character string operation for the realm authentication, realm-list handling, and external client login services workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: value.
      */
    private static string ReverseFourCharacterString(ReadOnlySpan<byte> value)
    {
        Span<byte> copy = stackalloc byte[4];
        value.CopyTo(copy);
        copy.Reverse();
        return Encoding.ASCII.GetString(copy).TrimEnd('\0');
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of RealmAuthSessionProcessor and keeps this workflow isolated from the caller.
      */
    private static byte GetLocaleIndex(string localeName)
    {
        return localeName switch
        {
            "enUS" => 0,
            "koKR" => 1,
            "frFR" => 2,
            "deDE" => 3,
            "zhCN" => 4,
            "zhTW" => 5,
            "esES" => 6,
            "esMX" => 7,
            "ruRU" => 8,
            _ => 0,
        };
    }

    /**
      * Represents immutable struct data passed between parts of the server.
      * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
      * Positional fields carried by this record: Build, OperatingSystem, LocaleName, Username.
      */
    private readonly record struct LogonChallenge(ushort Build, string OperatingSystem, string LocaleName, string Username);
}
