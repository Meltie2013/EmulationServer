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
// File: src/EmulationServer.Game/Data/Maps/MapTileCollisionReader.cs
// Purpose: Contains map tile collision reader code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Shared.Data.MapStore;

namespace EmulationServer.Game.Data.Maps;

// Type: MapTileCollisionReader
// Purpose: Provides map tile collision reader behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class MapTileCollisionReader
{
    // Method: Read
    // Purpose: Retrieves read data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - file: File value supplied by the caller for this operation.
    // Returns: Returns the map tile collision data value produced by this operation.
    // Notes: This keeps the operation scoped to MapTileCollisionReader so callers do not duplicate validation, protocol, or persistence rules.
    public static MapTileCollisionData Read(MapStoreFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        using MemoryStream stream = new(file.Payload, writable: false);
        using BinaryReader reader = new(stream);

        string magic = MapStoreBinaryPrimitives.ReadAscii(reader, MapStorePayloadConstants.CollisionPayloadMagic.Length, "collision payload magic value");
        if (!string.Equals(magic, MapStorePayloadConstants.CollisionPayloadMagic, StringComparison.Ordinal))
        {
            throw new MapFormatException($"{file.Path} has invalid collision payload magic '{magic}'.");
        }

        uint version = reader.ReadUInt32();
        ushort build = reader.ReadUInt16();
        uint mapId = reader.ReadUInt32();
        int tileX = reader.ReadInt32();
        int tileY = reader.ReadInt32();
        int placementCount = reader.ReadInt32();

        if (mapId != file.Header.MapId || tileX != file.Header.TileX || tileY != file.Header.TileY)
        {
            string expectedKey = MapStoreFileNames.FormatTileKey(file.Header.MapId, file.Header.TileX, file.Header.TileY);
            string actualKey = MapStoreFileNames.FormatTileKey(mapId, tileX, tileY);
            throw new MapFormatException($"{file.Path} has mismatched inner collision key. Expected {expectedKey}, got {actualKey}.");
        }

        if (build != file.Header.Build)
        {
            throw new MapFormatException($"{file.Path} has mismatched inner collision build. Outer={file.Header.Build}, Inner={build}.");
        }

        if (placementCount < 0)
        {
            throw new MapFormatException($"{file.Path} has invalid negative collision placement count {placementCount}.");
        }

        List<MapTileCollisionPlacement> placements = new(placementCount);
        for (int i = 0; i < placementCount; i++)
        {
            string modelKey = MapStoreBinaryPrimitives.ReadUtf8String(reader, file.Path, "collision model key");
            string normalizedPath = MapStoreBinaryPrimitives.ReadUtf8String(reader, file.Path, "collision model path");
            uint uniqueId = reader.ReadUInt32();
            MapTileVector3 position = ReadVector(reader);
            MapTileVector3 rotation = ReadVector(reader);
            MapTileBounds bounds = new(ReadVector(reader), ReadVector(reader));
            uint flags = reader.ReadUInt32();
            ushort doodadSet = reader.ReadUInt16();
            ushort nameSet = reader.ReadUInt16();

            placements.Add(new MapTileCollisionPlacement(modelKey, normalizedPath, uniqueId, position, rotation, bounds, flags, doodadSet, nameSet));
        }

        if (stream.Position != stream.Length)
        {
            throw new MapFormatException($"{file.Path} has {stream.Length - stream.Position} unread collision payload byte(s).");
        }

        MapTileKey key = new(file.Header.MapId, file.Header.TileX, file.Header.TileY);
        return new MapTileCollisionData(key, file.Header.Build, version, placements);
    }

    // Method: ReadVector
    // Purpose: Retrieves read vector data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - reader: Database reader used to execute this operation without opening unnecessary additional state.
    // Returns: Returns the map tile vector3 value produced by this operation.
    // Notes: This keeps the operation scoped to MapTileCollisionReader so callers do not duplicate validation, protocol, or persistence rules.
    private static MapTileVector3 ReadVector(BinaryReader reader)
    {
        return new MapTileVector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }
}
