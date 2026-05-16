
using System.Buffers.Binary;
using System.Numerics;
using System.Text;

using EmulationServer.Database.Accounts;
using EmulationServer.Network.Networking.Sessions;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.RealmServer.Auth;

public sealed class RealmAuthSessionProcessor : IRealmSessionProcessor
{
    private readonly AccountRepository _accountRepository;
    private readonly RealmListPacketBuilder _realmListPacketBuilder;

    private RealmAuthStatus _status = RealmAuthStatus.Challenge;
    private AccountLogonRecord? _account;
    private string _login = string.Empty;
    private string _os = string.Empty;
    private string _localeName = "enUS";
    private byte _locale;
    private ushort _build;
    private BigInteger _salt;
    private BigInteger _verifier;
    private BigInteger _hostPrivateEphemeral;
    private BigInteger _hostPublicEphemeral;
    private byte[] _sessionKey = [];

    public RealmAuthSessionProcessor(AccountRepository accountRepository, RealmListPacketBuilder realmListPacketBuilder)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _realmListPacketBuilder = realmListPacketBuilder ?? throw new ArgumentNullException(nameof(realmListPacketBuilder));
    }

    public async Task ProcessAsync(RealmSessionContext context, CancellationToken cancellationToken)
    {
        Logger.Write(LogType.NETWORK, $"Realm auth session started for {context.RemoteEndPoint}.", nameof(RealmAuthSessionProcessor));

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

                case RealmAuthOpCode.RealmList when _status == RealmAuthStatus.Authenticated:
                    await HandleRealmListAsync(context, cancellationToken);
                    break;

                default:
                    Logger.Write(LogType.WARNING, $"Received unauthorized RealmServer auth command 0x{command:X2} from {context.RemoteEndPoint} while status is {_status}.", nameof(RealmAuthSessionProcessor));
                    _status = RealmAuthStatus.Closed;
                    break;
            }
        }
    }

    private async Task HandleLogonChallengeAsync(RealmSessionContext context, CancellationToken cancellationToken)
    {
        byte protocolVersion = await context.ReadByteAsync(cancellationToken);
        byte[] sizeBytes = await context.ReadBytesAsync(2, cancellationToken);
        ushort remaining = BinaryPrimitives.ReadUInt16LittleEndian(sizeBytes);

        if (remaining < 30)
        {
            Logger.Write(LogType.WARNING, $"Invalid logon challenge size '{remaining}' from {context.RemoteEndPoint}.", nameof(RealmAuthSessionProcessor));
            _status = RealmAuthStatus.Closed;
            return;
        }

        Logger.Write(LogType.TRACE, $"Received auth protocol version 0x{protocolVersion:X2} with logon challenge size {remaining} from {context.RemoteEndPoint}.", nameof(RealmAuthSessionProcessor));

        byte[] payload = await context.ReadBytesAsync(remaining, cancellationToken);
        if (!TryParseLogonChallenge(payload, out LogonChallenge challenge))
        {
            await SendChallengeFailureAsync(context, RealmAuthResult.Failed, cancellationToken);
            _status = RealmAuthStatus.Closed;
            return;
        }

        _login = AccountRepository.NormalizeUsername(challenge.Username);
        _build = challenge.Build;
        _os = challenge.OperatingSystem;
        _localeName = challenge.LocaleName;
        _locale = GetLocaleIndex(_localeName);

        Logger.Write(LogType.NETWORK, $"Received logon challenge for account '{_login}' using client build {_build} from {context.RemoteEndPoint}.", nameof(RealmAuthSessionProcessor));

        if (!RealmBuilds.IsSupported(_build))
        {
            await SendChallengeFailureAsync(context, RealmAuthResult.VersionInvalid, cancellationToken);
            _status = RealmAuthStatus.Closed;
            return;
        }

        if (await _accountRepository.IsIpBannedAsync(context.RemoteAddress, cancellationToken))
        {
            Logger.Write(LogType.WARNING, $"Banned IP '{context.RemoteAddress}' attempted to authenticate.", nameof(RealmAuthSessionProcessor));
            await SendChallengeFailureAsync(context, RealmAuthResult.Banned, cancellationToken);
            _status = RealmAuthStatus.Closed;
            return;
        }

        _account = await _accountRepository.GetForLogonAsync(_login, cancellationToken);
        if (_account is null)
        {
            Logger.Write(LogType.WARNING, $"Unknown account '{_login}' attempted to authenticate.", nameof(RealmAuthSessionProcessor));
            await SendChallengeFailureAsync(context, RealmAuthResult.UnknownAccount, cancellationToken);
            _status = RealmAuthStatus.Closed;
            return;
        }

        if (_account.Locked && !string.Equals(_account.LastIp, context.RemoteAddress, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.WARNING, $"Locked account '{_login}' attempted to login from invalid IP '{context.RemoteAddress}'.", nameof(RealmAuthSessionProcessor));
            await SendChallengeFailureAsync(context, RealmAuthResult.LockedEnforced, cancellationToken);
            _status = RealmAuthStatus.Closed;
            return;
        }

        AccountBanStatus banStatus = await _accountRepository.GetAccountBanStatusAsync(_account.Id, cancellationToken);
        if (banStatus.IsBanned)
        {
            Logger.Write(LogType.WARNING, $"Banned account '{_login}' attempted to authenticate.", nameof(RealmAuthSessionProcessor));
            await SendChallengeFailureAsync(context, banStatus.IsPermanent ? RealmAuthResult.Banned : RealmAuthResult.Suspended, cancellationToken);
            _status = RealmAuthStatus.Closed;
            return;
        }

        await PrepareSrpChallengeAsync(_account, cancellationToken);
        await SendChallengeSuccessAsync(context, cancellationToken);

        _status = RealmAuthStatus.LogonProof;
    }

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
            Logger.Write(LogType.WARNING, $"Account '{_login}' sent invalid SRP6 client public ephemeral.", nameof(RealmAuthSessionProcessor));
            _status = RealmAuthStatus.Closed;
            return;
        }

        BigInteger scrambler = Srp6Utilities.CalculateScrambler(clientPublicEphemeral, _hostPublicEphemeral);
        BigInteger sessionSecret = Srp6Utilities.CalculateSessionSecret(clientPublicEphemeral, _verifier, scrambler, _hostPrivateEphemeral);
        _sessionKey = Srp6Utilities.HashSessionKey(sessionSecret);

        byte[] expectedProof = Srp6Utilities.CalculateClientProof(_login, _salt, clientPublicEphemeral, _hostPublicEphemeral, _sessionKey);

        if (!Srp6Utilities.FixedTimeEquals(expectedProof, clientProof))
        {
            Logger.Write(LogType.WARNING, $"Account '{_login}' failed SRP6 proof validation.", nameof(RealmAuthSessionProcessor));
            await _accountRepository.IncrementFailedLoginsAsync(_login, cancellationToken);
            await SendProofFailureAsync(context, cancellationToken);
            _status = RealmAuthStatus.Closed;
            return;
        }

        string sessionKeyHex = Convert.ToHexString(_sessionKey).ToLowerInvariant();
        await _accountRepository.UpdateSuccessfulLoginAsync(_login, sessionKeyHex, context.RemoteAddress, _locale, _os, cancellationToken);

        byte[] hostProof = Srp6Utilities.CalculateHostProof(clientPublicEphemeral, clientProof, _sessionKey);
        await SendProofSuccessAsync(context, hostProof, cancellationToken);

        Logger.Write(LogType.SUCCESS, $"Account '{_login}' authenticated successfully from {context.RemoteEndPoint}.", nameof(RealmAuthSessionProcessor));

        _status = RealmAuthStatus.Authenticated;
    }

    private async Task HandleRealmListAsync(RealmSessionContext context, CancellationToken cancellationToken)
    {
        _ = await context.ReadBytesAsync(4, cancellationToken);

        if (_account is null)
        {
            _status = RealmAuthStatus.Closed;
            return;
        }

        byte[] packet = _realmListPacketBuilder.BuildRealmList(_build, _account.GmLevel);
        await context.WriteAsync(packet, cancellationToken);

        Logger.Write(LogType.TRACE, $"Sent realm list to account '{_login}'.", nameof(RealmAuthSessionProcessor));
    }

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

    private async Task SendChallengeFailureAsync(RealmSessionContext context, RealmAuthResult result, CancellationToken cancellationToken)
    {
        ByteWriter packet = new();
        packet.WriteUInt8((byte)RealmAuthOpCode.AuthLogonChallenge);
        packet.WriteUInt8(0);
        packet.WriteUInt8((byte)result);

        await context.WriteAsync(packet.ToArray(), cancellationToken);
    }

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

    private static string ReverseFourCharacterString(ReadOnlySpan<byte> value)
    {
        Span<byte> copy = stackalloc byte[4];
        value.CopyTo(copy);
        copy.Reverse();
        return Encoding.ASCII.GetString(copy).TrimEnd('\0');
    }

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

    private readonly record struct LogonChallenge(ushort Build, string OperatingSystem, string LocaleName, string Username);
}
