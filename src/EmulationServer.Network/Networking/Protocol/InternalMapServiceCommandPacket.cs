using System.Globalization;

namespace EmulationServer.Network.Networking.Protocol;

public sealed record InternalMapServiceCommandPacket(
    string CommandId,
    string Action,
    int MapId)
{
    public string ToPacketLine()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{InternalProtocol.MapServiceCommand} {CommandId} {Action} {MapId}");
    }

    public static bool TryParse(string packet, out InternalMapServiceCommandPacket command)
    {
        command = Empty;

        if (string.IsNullOrWhiteSpace(packet))
        {
            return false;
        }

        string[] parts = packet.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 || !string.Equals(parts[0], InternalProtocol.MapServiceCommand, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(parts[1]) || string.IsNullOrWhiteSpace(parts[2]))
        {
            return false;
        }

        if (!int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int mapId) || mapId < 0)
        {
            return false;
        }

        command = new InternalMapServiceCommandPacket(parts[1], parts[2], mapId);
        return true;
    }

    private static InternalMapServiceCommandPacket Empty { get; } = new(string.Empty, string.Empty, 0);
}
