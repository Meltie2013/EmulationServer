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

using System.Globalization;
using System.Text;
using EmulationServer.Game.WorldData;
using EmulationServer.Network.Networking.Protocol;

namespace EmulationServer.Game.GameObjects;

/**
  * Serializes and parses the internal game object snapshot protocol used by WorldServer, MapServer, and InstanceServer.
  * The packets are intentionally line-oriented and chunked so MapServer and InstanceServer can receive world data without a database connection.
  */
public static class GameObjectSnapshotProtocol
{
    private const string EncodedEmptyString = "AA==";

    public static string CreateBeginPacket(string snapshotId, int mapId, int templateCount, int spawnCount)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{InternalProtocol.GameObjectSnapshotBegin} {snapshotId} {mapId} {templateCount} {spawnCount}");
    }

    public static string CreateTemplatePacket(string snapshotId, GameObjectTemplateRecord template)
    {
        ArgumentNullException.ThrowIfNull(template);

        string dataFields = string.Join(',', template.DataFields.Select(value => value.ToString(CultureInfo.InvariantCulture)));
        string encodedDataFields = Encode(dataFields);
        string encodedName = Encode(template.Name);
        string encodedScriptName = Encode(template.ScriptName);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{InternalProtocol.GameObjectTemplateSnapshot} {snapshotId} {template.Entry} {template.Type} {template.DisplayId} {template.Faction} {template.Flags} {template.Size:0.######} {template.MinGold} {template.MaxGold} {encodedDataFields} {encodedName} {encodedScriptName}");
    }

    public static string CreateSpawnPacket(string snapshotId, GameObjectSpawnRecord spawn)
    {
        ArgumentNullException.ThrowIfNull(spawn);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{InternalProtocol.GameObjectSpawnSnapshot} {snapshotId} {spawn.Guid} {spawn.Entry} {spawn.Map} {spawn.ZoneId} {spawn.AreaId} {spawn.PositionX:0.######} {spawn.PositionY:0.######} {spawn.PositionZ:0.######} {spawn.Orientation:0.######} {spawn.Rotation0:0.######} {spawn.Rotation1:0.######} {spawn.Rotation2:0.######} {spawn.Rotation3:0.######} {spawn.SpawnTimeSeconds} {spawn.AnimProgress} {spawn.State}");
    }

    public static string CreateEndPacket(string snapshotId, int mapId)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{InternalProtocol.GameObjectSnapshotEnd} {snapshotId} {mapId}");
    }

    public static bool TryParseBegin(string packet, out string snapshotId, out int mapId, out int templateCount, out int spawnCount)
    {
        snapshotId = string.Empty;
        mapId = 0;
        templateCount = 0;
        spawnCount = 0;

        string[] parts = Split(packet);
        if (parts.Length != 5 || !IsOpcode(parts[0], InternalProtocol.GameObjectSnapshotBegin))
        {
            return false;
        }

        if (!TryParseNonNegativeInt(parts[2], out mapId) ||
            !TryParseNonNegativeInt(parts[3], out templateCount) ||
            !TryParseNonNegativeInt(parts[4], out spawnCount))
        {
            return false;
        }

        snapshotId = parts[1];
        return !string.IsNullOrWhiteSpace(snapshotId);
    }

    public static bool TryParseTemplate(string packet, out string snapshotId, out GameObjectTemplateRecord template)
    {
        snapshotId = string.Empty;
        template = EmptyTemplate;

        string[] parts = Split(packet);
        if ((parts.Length != 12 && parts.Length != 13) || !IsOpcode(parts[0], InternalProtocol.GameObjectTemplateSnapshot))
        {
            return false;
        }

        if (!uint.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint entry) ||
            !byte.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte type) ||
            !uint.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint displayId) ||
            !ushort.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort faction) ||
            !uint.TryParse(parts[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint flags) ||
            !float.TryParse(parts[7], NumberStyles.Float, CultureInfo.InvariantCulture, out float size) ||
            !uint.TryParse(parts[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint minGold) ||
            !uint.TryParse(parts[9], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint maxGold))
        {
            return false;
        }

        string encodedScriptName = parts.Length == 13
            ? parts[12]
            : EncodedEmptyString;

        if (!TryDecode(parts[10], out string dataFieldText) ||
            !TryDecode(parts[11], out string name) ||
            !TryDecode(encodedScriptName, out string scriptName))
        {
            return false;
        }

        uint[] dataFields = ParseDataFields(dataFieldText);
        snapshotId = parts[1];
        template = new GameObjectTemplateRecord(
            entry,
            type,
            displayId,
            name,
            faction,
            flags,
            size,
            dataFields,
            minGold,
            maxGold,
            scriptName);

        return !string.IsNullOrWhiteSpace(snapshotId);
    }

    public static bool TryParseSpawn(string packet, out string snapshotId, out GameObjectSpawnRecord spawn)
    {
        snapshotId = string.Empty;
        spawn = EmptySpawn;

        string[] parts = Split(packet);
        if (parts.Length != 18 || !IsOpcode(parts[0], InternalProtocol.GameObjectSpawnSnapshot))
        {
            return false;
        }

        if (!uint.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint guid) ||
            !uint.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint entry) ||
            !ushort.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort map) ||
            !uint.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint zoneId) ||
            !uint.TryParse(parts[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint areaId) ||
            !float.TryParse(parts[7], NumberStyles.Float, CultureInfo.InvariantCulture, out float positionX) ||
            !float.TryParse(parts[8], NumberStyles.Float, CultureInfo.InvariantCulture, out float positionY) ||
            !float.TryParse(parts[9], NumberStyles.Float, CultureInfo.InvariantCulture, out float positionZ) ||
            !float.TryParse(parts[10], NumberStyles.Float, CultureInfo.InvariantCulture, out float orientation) ||
            !float.TryParse(parts[11], NumberStyles.Float, CultureInfo.InvariantCulture, out float rotation0) ||
            !float.TryParse(parts[12], NumberStyles.Float, CultureInfo.InvariantCulture, out float rotation1) ||
            !float.TryParse(parts[13], NumberStyles.Float, CultureInfo.InvariantCulture, out float rotation2) ||
            !float.TryParse(parts[14], NumberStyles.Float, CultureInfo.InvariantCulture, out float rotation3) ||
            !int.TryParse(parts[15], NumberStyles.Integer, CultureInfo.InvariantCulture, out int spawnTimeSeconds) ||
            !byte.TryParse(parts[16], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte animProgress) ||
            !byte.TryParse(parts[17], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte state))
        {
            return false;
        }

        snapshotId = parts[1];
        spawn = new GameObjectSpawnRecord(
            guid,
            entry,
            map,
            zoneId,
            areaId,
            positionX,
            positionY,
            positionZ,
            orientation,
            rotation0,
            rotation1,
            rotation2,
            rotation3,
            spawnTimeSeconds,
            animProgress,
            state);

        return !string.IsNullOrWhiteSpace(snapshotId);
    }

    public static bool TryParseEnd(string packet, out string snapshotId, out int mapId)
    {
        snapshotId = string.Empty;
        mapId = 0;

        string[] parts = Split(packet);
        if (parts.Length != 3 || !IsOpcode(parts[0], InternalProtocol.GameObjectSnapshotEnd))
        {
            return false;
        }

        if (!TryParseNonNegativeInt(parts[2], out mapId))
        {
            return false;
        }

        snapshotId = parts[1];
        return !string.IsNullOrWhiteSpace(snapshotId);
    }

    public static bool IsSnapshotPacket(string packet)
    {
        string[] parts = Split(packet);
        return parts.Length > 0 &&
            (IsOpcode(parts[0], InternalProtocol.GameObjectSnapshotBegin) ||
             IsOpcode(parts[0], InternalProtocol.GameObjectTemplateSnapshot) ||
             IsOpcode(parts[0], InternalProtocol.GameObjectSpawnSnapshot) ||
             IsOpcode(parts[0], InternalProtocol.GameObjectSnapshotEnd));
    }

    private static uint[] ParseDataFields(string value)
    {
        uint[] dataFields = new uint[GameObjectTemplateRecord.DataFieldCount];
        if (string.IsNullOrWhiteSpace(value))
        {
            return dataFields;
        }

        string[] parts = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        int count = Math.Min(parts.Length, dataFields.Length);
        for (int index = 0; index < count; index++)
        {
            if (uint.TryParse(parts[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint parsed))
            {
                dataFields[index] = parsed;
            }
        }

        return dataFields;
    }

    private static bool TryParseNonNegativeInt(string value, out int result)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) && result >= 0;
    }

    private static string Encode(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return EncodedEmptyString;
        }

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    private static bool TryDecode(string value, out string decoded)
    {
        decoded = string.Empty;
        if (string.Equals(value, EncodedEmptyString, StringComparison.Ordinal))
        {
            return true;
        }

        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(value));
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsOpcode(string value, string expected)
    {
        return string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static string[] Split(string packet)
    {
        return string.IsNullOrWhiteSpace(packet)
            ? []
            : packet.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static GameObjectTemplateRecord EmptyTemplate { get; } = new(
        0,
        0,
        0,
        string.Empty,
        0,
        0,
        1.0f,
        new uint[GameObjectTemplateRecord.DataFieldCount],
        0,
        0,
        string.Empty);

    private static GameObjectSpawnRecord EmptySpawn { get; } = new(
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0);
}
