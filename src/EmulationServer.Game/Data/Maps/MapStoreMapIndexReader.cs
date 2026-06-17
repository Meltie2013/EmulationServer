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
// File: src/EmulationServer.Game/Data/Maps/MapStoreMapIndexReader.cs
// Purpose: Contains map store map index reader code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Shared.Data.MapStore;

namespace EmulationServer.Game.Data.Maps;

// Type: MapStoreMapIndexReader
// Purpose: Provides map store map index reader behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class MapStoreMapIndexReader
{

    // Method: Read
    // Purpose: Retrieves read data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - path: Path value supplied by the caller for this operation.
    // - expectedMapId: Expected map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the map store map index value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreMapIndexReader so callers do not duplicate validation, protocol, or persistence rules.
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
