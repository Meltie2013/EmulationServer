
using System.Net;

namespace EmulationServer.Network.Configuration;

public sealed class RealmSocketListenerSettings
{
    public string BindAddress { get; init; } = "0.0.0.0";

    public int Port { get; init; } = 3724;

    public int Backlog { get; init; } = 128;

    public int MaxConnections { get; init; } = 1024;

    public TimeSpan ShutdownGracePeriod { get; init; } = TimeSpan.FromSeconds(15);

    public IPAddress GetBindAddress()
    {
        if (!IPAddress.TryParse(BindAddress, out IPAddress? ipAddress))
        {
            throw new InvalidOperationException($"Invalid realm bind address: '{BindAddress}'.");
        }

        return ipAddress;
    }

    public void Validate()
    {
        _ = GetBindAddress();

        if (Port is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
        {
            throw new InvalidOperationException($"Invalid realm port: {Port}. Valid range is 0-65535.");
        }

        if (Backlog <= 0)
        {
            throw new InvalidOperationException("Realm listener backlog must be greater than zero.");
        }

        if (MaxConnections <= 0)
        {
            throw new InvalidOperationException("Realm max connections must be greater than zero.");
        }

        if (ShutdownGracePeriod < TimeSpan.Zero)
        {
            throw new InvalidOperationException("Realm shutdown grace period cannot be negative.");
        }
    }
}
