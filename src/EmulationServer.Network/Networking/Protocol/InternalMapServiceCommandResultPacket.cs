using System.Globalization;
using System.Text;

namespace EmulationServer.Network.Networking.Protocol;

public sealed record InternalMapServiceCommandResultPacket(
    string CommandId,
    string OwnerServerName,
    string Kind,
    int MapId,
    long InstanceId,
    string ResultCode,
    string State,
    string Message)
{
    public string ToPacketLine()
    {
        string encodedMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(Message ?? string.Empty));

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{InternalProtocol.MapServiceCommandResult} {CommandId} {OwnerServerName} {Kind} {MapId} {InstanceId} {ResultCode} {State} {encodedMessage}");
    }

    public static bool TryParse(string packet, out InternalMapServiceCommandResultPacket result)
    {
        result = Empty;

        if (string.IsNullOrWhiteSpace(packet))
        {
            return false;
        }

        string[] parts = packet.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 9 || !string.Equals(parts[0], InternalProtocol.MapServiceCommandResult, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int mapId) || mapId < 0)
        {
            return false;
        }

        if (!long.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out long instanceId) || instanceId < 0)
        {
            return false;
        }

        string message;
        try
        {
            message = Encoding.UTF8.GetString(Convert.FromBase64String(parts[8]));
        }
        catch (FormatException)
        {
            return false;
        }

        result = new InternalMapServiceCommandResultPacket(
            parts[1],
            parts[2],
            parts[3],
            mapId,
            instanceId,
            parts[6],
            parts[7],
            message);

        return true;
    }

    private static InternalMapServiceCommandResultPacket Empty { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        0,
        0,
        string.Empty,
        string.Empty,
        string.Empty);
}
