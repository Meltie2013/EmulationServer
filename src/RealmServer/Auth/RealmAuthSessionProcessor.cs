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
// File: src/RealmServer/Auth/RealmAuthSessionProcessor.cs
// Purpose: Contains realm auth session processor code for the realm server authentication, realm-list, and account connection layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

using EmulationServer.Database.Accounts;
using EmulationServer.Network.Networking.Sessions;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.RealmServer.Auth;

// Type: RealmAuthSessionProcessor
// Purpose: Provides realm auth session processor behavior for the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class RealmAuthSessionProcessor : IRealmSessionProcessor
{

    // Method: FromMilliseconds
    // Purpose: Executes the from milliseconds operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: Returns the time span terminal auth failure delivery delay = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
    private static readonly TimeSpan TerminalAuthFailureDeliveryDelay = TimeSpan.FromMilliseconds(250);

    // Field: Stores the account repository state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current account repository backing value maintained by the owning type.
    private readonly AccountRepository _accountRepository;

    // Field: Stores the realm list packet builder state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current realm list packet builder backing value maintained by the owning type.
    private readonly RealmListPacketBuilder _realmListPacketBuilder;

    // Field: Stores the status state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current status backing value maintained by the owning type.
    private RealmAuthStatus _status = RealmAuthStatus.Challenge;

    // Field: Stores the account state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current account backing value maintained by the owning type.
    private AccountLogonRecord? _account;

    // Field: Stores the login state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current login backing value maintained by the owning type.
    private string _login = string.Empty;

    // Field: Stores the os state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current os backing value maintained by the owning type.
    private string _os = string.Empty;

    // Field: Stores the locale name state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current locale name backing value maintained by the owning type.
    private string _localeName = "enUS";

    // Field: Stores the locale state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current locale backing value maintained by the owning type.
    private byte _locale;

    // Field: Stores the build state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current build backing value maintained by the owning type.
    private ushort _build;

    // Field: Stores the salt state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current salt backing value maintained by the owning type.
    private BigInteger _salt;

    // Field: Stores the verifier state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current verifier backing value maintained by the owning type.
    private BigInteger _verifier;

    // Field: Stores the host private ephemeral state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current host private ephemeral backing value maintained by the owning type.
    private BigInteger _hostPrivateEphemeral;

    // Field: Stores the host public ephemeral state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current host public ephemeral backing value maintained by the owning type.
    private BigInteger _hostPublicEphemeral;

    // Field: Stores the session key state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current session key backing value maintained by the owning type.
    private byte[] _sessionKey = [];

    // Field: Stores the reconnect challenge state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current reconnect challenge backing value maintained by the owning type.
    private byte[] _reconnectChallenge = [];

    // Field: Stores the reconnect checksum salt state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current reconnect checksum salt backing value maintained by the owning type.
    private byte[] _reconnectChecksumSalt = [];

    // Constructor: RealmAuthSessionProcessor
    // Purpose: Initializes a new RealmAuthSessionProcessor instance with dependencies and values required by the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - accountRepository: Account repository value supplied by the caller for this operation.
    // - realmListPacketBuilder: Realm list packet builder value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
    public RealmAuthSessionProcessor(AccountRepository accountRepository, RealmListPacketBuilder realmListPacketBuilder)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException();
        _realmListPacketBuilder = realmListPacketBuilder ?? throw new ArgumentNullException();
    }

    // Method: ProcessAsync
    // Purpose: Executes the process operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: HandleLogonChallengeAsync
    // Purpose: Handles handle logon challenge work for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: HandleReconnectChallengeAsync
    // Purpose: Handles handle reconnect challenge work for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: HandleLogonProofAsync
    // Purpose: Handles handle logon proof work for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: HandleReconnectProofAsync
    // Purpose: Handles handle reconnect proof work for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: HandleRealmListAsync
    // Purpose: Handles handle realm list work for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: PrepareSrpChallengeAsync
    // Purpose: Executes the prepare srp challenge operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - account: Account value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: SendChallengeFailureAsync
    // Purpose: Handles send challenge failure work for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - result: Result value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task SendChallengeFailureAsync(RealmSessionContext context, RealmAuthResult result, CancellationToken cancellationToken)
    {
        ByteWriter packet = new();
        packet.WriteUInt8((byte)RealmAuthOpCode.AuthLogonChallenge);
        packet.WriteUInt8(0);
        packet.WriteUInt8((byte)result);

        await context.WriteAsync(packet.ToArray(), cancellationToken);
    }

    // Method: SendReconnectChallengeFailureAsync
    // Purpose: Handles send reconnect challenge failure work for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - result: Result value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task SendReconnectChallengeFailureAsync(RealmSessionContext context, RealmAuthResult result, CancellationToken cancellationToken)
    {
        ByteWriter packet = new();
        packet.WriteUInt8((byte)RealmAuthOpCode.AuthReconnectChallenge);
        packet.WriteUInt8((byte)result);

        await context.WriteAsync(packet.ToArray(), cancellationToken);
    }

    // Method: SendReconnectChallengeSuccessAsync
    // Purpose: Handles send reconnect challenge success work for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task SendReconnectChallengeSuccessAsync(RealmSessionContext context, CancellationToken cancellationToken)
    {
        ByteWriter packet = new();
        packet.WriteUInt8((byte)RealmAuthOpCode.AuthReconnectChallenge);
        packet.WriteUInt8((byte)RealmAuthResult.Success);
        packet.WriteBytes(_reconnectChallenge);
        packet.WriteBytes(_reconnectChecksumSalt);

        await context.WriteAsync(packet.ToArray(), cancellationToken);
    }

    // Method: SendChallengeFailureAndCloseAsync
    // Purpose: Handles send challenge failure and close work for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - result: Result value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task SendChallengeFailureAndCloseAsync(RealmSessionContext context, RealmAuthResult result, CancellationToken cancellationToken)
    {
        await SendChallengeFailureAsync(context, result, cancellationToken);
        await RealmSessionContext.AllowTerminalResponseDeliveryAsync(TerminalAuthFailureDeliveryDelay, cancellationToken);
        _status = RealmAuthStatus.Closed;
    }

    // Method: SendReconnectChallengeFailureAndCloseAsync
    // Purpose: Handles send reconnect challenge failure and close work for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - result: Result value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task SendReconnectChallengeFailureAndCloseAsync(RealmSessionContext context, RealmAuthResult result, CancellationToken cancellationToken)
    {
        await SendReconnectChallengeFailureAsync(context, result, cancellationToken);
        await RealmSessionContext.AllowTerminalResponseDeliveryAsync(TerminalAuthFailureDeliveryDelay, cancellationToken);
        _status = RealmAuthStatus.Closed;
    }

    // Method: SendProofFailureAndCloseAsync
    // Purpose: Handles send proof failure and close work for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task SendProofFailureAndCloseAsync(RealmSessionContext context, CancellationToken cancellationToken)
    {
        await SendProofFailureAsync(context, cancellationToken);
        await RealmSessionContext.AllowTerminalResponseDeliveryAsync(TerminalAuthFailureDeliveryDelay, cancellationToken);
        _status = RealmAuthStatus.Closed;
    }

    // Method: SendChallengeSuccessAsync
    // Purpose: Handles send challenge success work for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: SendProofSuccessAsync
    // Purpose: Handles send proof success work for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - bytehostProof: Bytehost proof value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: SendProofFailureAsync
    // Purpose: Handles send proof failure work for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: SendReconnectProofSuccessAsync
    // Purpose: Handles send reconnect proof success work for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: SendReconnectProofFailureAsync
    // Purpose: Handles send reconnect proof failure work for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: SendReconnectProofFailureAndCloseAsync
    // Purpose: Handles send reconnect proof failure and close work for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task SendReconnectProofFailureAndCloseAsync(RealmSessionContext context, CancellationToken cancellationToken)
    {
        await SendReconnectProofFailureAsync(context, cancellationToken);
        await RealmSessionContext.AllowTerminalResponseDeliveryAsync(TerminalAuthFailureDeliveryDelay, cancellationToken);
        _status = RealmAuthStatus.Closed;
    }

    // Method: CalculateReconnectProof
    // Purpose: Calculates calculate reconnect proof values for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - byteproofData: Byteproof data value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: TryParseSessionKey
    // Purpose: Attempts to retrieve or parse try parse session key data without treating normal misses as failures.
    // Parameters:
    // - sessionKeyHex: Session key hex value supplied by the caller for this operation.
    // - bytesessionKey: Bytesession key value supplied by the caller for this operation.
    // Returns: Returns true when try parse session key succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: TryParseLogonChallenge
    // Purpose: Attempts to retrieve or parse try parse logon challenge data without treating normal misses as failures.
    // Parameters:
    // - bytepayload: Bytepayload value supplied by the caller for this operation.
    // - challenge: Challenge value supplied by the caller for this operation.
    // Returns: Returns true when try parse logon challenge succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReverseFourCharacterString
    // Purpose: Executes the reverse four character string operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
    private static string ReverseFourCharacterString(ReadOnlySpan<byte> value)
    {
        Span<byte> copy = stackalloc byte[4];
        value.CopyTo(copy);
        copy.Reverse();
        return Encoding.ASCII.GetString(copy).TrimEnd('\0');
    }

    // Method: GetLocaleIndex
    // Purpose: Retrieves get locale index data for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - localeName: Locale name value supplied by the caller for this operation.
    // Returns: Returns the byte value produced by this operation.
    // Notes: This keeps the operation scoped to RealmAuthSessionProcessor so callers do not duplicate validation, protocol, or persistence rules.
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

    // Type: LogonChallenge
    // Purpose: Represents logon challenge data passed through the realm server authentication, realm-list, and account connection layer.
    // Constructor values:
    // - Build: Build value supplied by the caller for this operation.
    // - OperatingSystem: Operating system value supplied by the caller for this operation.
    // - LocaleName: Locale name value supplied by the caller for this operation.
    // - Username: Username value supplied by the caller for this operation.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
    private readonly record struct LogonChallenge(ushort Build, string OperatingSystem, string LocaleName, string Username);
}
