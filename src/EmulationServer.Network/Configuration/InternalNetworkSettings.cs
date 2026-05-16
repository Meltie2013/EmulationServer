
using System.Net;

namespace EmulationServer.Network.Configuration;

public sealed class InternalNetworkSettings
{
    public string ServerName { get; init; } = "Server";

    public string BindAddress { get; init; } = "127.0.0.1";

    public int Port { get; init; } = 0;

    public string RegistrationKey { get; init; } = string.Empty;

    public int Backlog { get; init; } = 128;

    public int MaxConnections { get; init; } = 1024;

    public TimeSpan ShutdownGracePeriod { get; init; } = TimeSpan.FromSeconds(15);

    public TimeSpan LatencyReportInterval { get; init; } = TimeSpan.FromSeconds(15);

    public TimeSpan PingTimeout { get; init; } = TimeSpan.FromSeconds(5);

    public IReadOnlyList<InternalPeerSettings> Peers { get; init; } = [];

    public IPAddress GetBindAddress()
    {
        if (!IPAddress.TryParse(BindAddress, out IPAddress? ipAddress))
        {
            throw new InvalidOperationException($"Invalid internal network bind address: '{BindAddress}'.");
        }

        return ipAddress;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ServerName))
        {
            throw new InvalidOperationException("Internal network server name is required.");
        }

        _ = GetBindAddress();

        if (Port is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
        {
            throw new InvalidOperationException($"Invalid internal network port: {Port}. Valid range is 0-65535.");
        }

        if (string.IsNullOrWhiteSpace(RegistrationKey))
        {
            throw new InvalidOperationException("Internal network registration key is required.");
        }

        if (RegistrationKey.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException("Internal network registration key cannot contain whitespace.");
        }

        if (RegistrationKey.Length > 256)
        {
            throw new InvalidOperationException("Internal network registration key cannot be longer than 256 characters.");
        }

        if (Backlog <= 0)
        {
            throw new InvalidOperationException("Internal network listener backlog must be greater than zero.");
        }

        if (MaxConnections <= 0)
        {
            throw new InvalidOperationException("Internal network max connections must be greater than zero.");
        }

        if (ShutdownGracePeriod < TimeSpan.Zero)
        {
            throw new InvalidOperationException("Internal network shutdown grace period cannot be negative.");
        }

        if (LatencyReportInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Internal network latency report interval must be greater than zero.");
        }

        if (PingTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Internal network ping timeout must be greater than zero.");
        }

        if (PingTimeout >= LatencyReportInterval)
        {
            throw new InvalidOperationException("Internal network ping timeout must be less than the latency report interval.");
        }

        HashSet<string> peerNames = new(StringComparer.OrdinalIgnoreCase);

        foreach (InternalPeerSettings peer in Peers)
        {
            peer.Validate();

            if (!peerNames.Add(peer.Name))
            {
                throw new InvalidOperationException($"Duplicate internal peer name: '{peer.Name}'.");
            }
        }
    }
}
