
namespace EmulationServer.Network.Configuration;

public sealed class InternalPeerSettings
{
    public string Name { get; init; } = string.Empty;

    public string Host { get; init; } = "127.0.0.1";

    public int Port { get; init; }

    public bool Enabled { get; init; } = true;

    public TimeSpan ReconnectDelay { get; init; } = TimeSpan.FromSeconds(5);

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException("Internal peer name is required.");
        }

        if (string.IsNullOrWhiteSpace(Host))
        {
            throw new InvalidOperationException($"Internal peer '{Name}' host is required.");
        }

        if (Port is < 1 or > 65535)
        {
            throw new InvalidOperationException($"Invalid internal peer '{Name}' port: {Port}. Valid range is 1-65535.");
        }

        if (ReconnectDelay <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"Internal peer '{Name}' reconnect delay must be greater than zero.");
        }
    }
}
