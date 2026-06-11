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

using EmulationServer.Shared.Data.MapStore;

namespace EmulationServer.Game.Data.Maps;

/**
  * Reads the typed liquid model from a validated mapstore liquid payload.
  */
public static class MapTileLiquidReader
{
    public static MapTileLiquidData Read(MapStoreFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        using MemoryStream stream = new(file.Payload, writable: false);
        using BinaryReader reader = new(stream);

        string payloadMagic = MapStoreBinaryPrimitives.ReadFourCC(reader, "liquid payload FourCC value");
        if (!string.Equals(payloadMagic, MapStorePayloadConstants.LiquidPayloadMagic, StringComparison.Ordinal))
        {
            throw new MapFormatException($"{file.Path} has invalid liquid payload magic '{payloadMagic}'.");
        }

        uint liquidSectionSize = reader.ReadUInt32();
        if (8L + liquidSectionSize != file.Payload.Length)
        {
            throw new MapFormatException($"{file.Path} has invalid liquid payload size. Header declares {liquidSectionSize} byte(s), payload contains {file.Payload.Length} byte(s).");
        }

        MapTileKey key = new(file.Header.MapId, file.Header.TileX, file.Header.TileY);
        if (liquidSectionSize == 0)
        {
            return new MapTileLiquidData(key, file.Header.Build, false, 0, 0, 0, 0, 0, 0, 0.0f, null, null, null);
        }

        string sectionMagic = MapStoreBinaryPrimitives.ReadFourCC(reader, "liquid section FourCC value");
        if (!string.Equals(sectionMagic, MapStorePayloadConstants.LiquidSectionMagic, StringComparison.Ordinal))
        {
            throw new MapFormatException($"{file.Path} has invalid liquid section magic '{sectionMagic}'.");
        }

        ushort flags = reader.ReadUInt16();
        ushort liquidType = reader.ReadUInt16();
        byte offsetX = reader.ReadByte();
        byte offsetY = reader.ReadByte();
        byte width = reader.ReadByte();
        byte height = reader.ReadByte();
        float liquidLevel = reader.ReadSingle();

        ushort[]? liquidTypeIds = null;
        byte[]? liquidFlags = null;
        if ((flags & MapStorePayloadConstants.MapLiquidNoType) == 0)
        {
            liquidTypeIds = new ushort[MapStorePayloadConstants.AreaCellCount];
            for (int i = 0; i < liquidTypeIds.Length; i++)
            {
                liquidTypeIds[i] = reader.ReadUInt16();
            }

            liquidFlags = new byte[MapStorePayloadConstants.AreaCellCount];
            for (int i = 0; i < liquidFlags.Length; i++)
            {
                liquidFlags[i] = reader.ReadByte();
            }
        }

        float[]? liquidHeights = null;
        if ((flags & MapStorePayloadConstants.MapLiquidNoHeight) == 0)
        {
            int heightSampleCount = checked(width * height);
            liquidHeights = new float[heightSampleCount];
            for (int i = 0; i < liquidHeights.Length; i++)
            {
                liquidHeights[i] = reader.ReadSingle();
            }
        }

        if (stream.Position != stream.Length)
        {
            throw new MapFormatException($"{file.Path} has {stream.Length - stream.Position} unread liquid payload byte(s).");
        }

        return new MapTileLiquidData(key, file.Header.Build, true, flags, liquidType, offsetX, offsetY, width, height, liquidLevel, liquidTypeIds, liquidFlags, liquidHeights);
    }
}
