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
  * Reads the per-map tile index used to preload every extracted grid during map service startup.
  */
public static class MapStoreMapIndexReader
{
    /**
      * Reads and validates one map.index.bin file.
      */
    public static MapStoreMapIndex Read(string path, uint expectedMapId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using FileStream stream = File.OpenRead(path);
        using BinaryReader reader = new(stream);

        string magic = MapStoreBinaryPrimitives.ReadFourCC(reader);
        if (!string.Equals(magic, MapStoreFormat.IndexMagic, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"{path} has invalid mapstore index magic '{magic}'. Expected '{MapStoreFormat.IndexMagic}'.");
        }

        ushort version = reader.ReadUInt16();
        if (version != MapStoreFormat.CurrentVersion)
        {
            throw new InvalidDataException($"{path} has unsupported mapstore index version {version}. Expected {MapStoreFormat.CurrentVersion}.");
        }

        ushort build = reader.ReadUInt16();
        uint mapId = reader.ReadUInt32();
        if (mapId != expectedMapId)
        {
            throw new InvalidDataException($"{path} belongs to map {mapId:D3}, but map {expectedMapId:D3} was requested.");
        }

        int recordCount = reader.ReadInt32();
        if (recordCount < 0)
        {
            throw new InvalidDataException($"{path} has invalid negative mapstore tile count {recordCount}.");
        }

        List<MapStoreMapIndexRecord> records = new(recordCount);
        for (int index = 0; index < recordCount; index++)
        {
            byte tileX = reader.ReadByte();
            byte tileY = reader.ReadByte();
            MapStoreTileDataFlags flags = (MapStoreTileDataFlags)reader.ReadByte();
            _ = reader.ReadByte();

            records.Add(new MapStoreMapIndexRecord(new MapTileKey(mapId, tileX, tileY), flags));
        }

        if (stream.Position != stream.Length)
        {
            throw new InvalidDataException($"{path} contains trailing bytes after the mapstore index records.");
        }

        return new MapStoreMapIndex(mapId, build, records);
    }
}
