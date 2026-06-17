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
// File: src/EmulationServer.Game/Data/Maps/MapTileNavmeshReader.cs
// Purpose: Contains map tile navmesh reader code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Shared.Data.MapStore;

namespace EmulationServer.Game.Data.Maps;

// Type: MapTileNavmeshReader
// Purpose: Provides map tile navmesh reader behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class MapTileNavmeshReader
{
    // Method: Read
    // Purpose: Retrieves read data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - file: File value supplied by the caller for this operation.
    // Returns: Returns the map tile navmesh data value produced by this operation.
    // Notes: This keeps the operation scoped to MapTileNavmeshReader so callers do not duplicate validation, protocol, or persistence rules.
    public static MapTileNavmeshData Read(MapStoreFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        using MemoryStream stream = new(file.Payload, writable: false);
        using BinaryReader reader = new(stream);

        string magic = MapStoreBinaryPrimitives.ReadFourCC(reader, "navmesh FourCC value");
        if (!string.Equals(magic, MapStorePayloadConstants.NavmeshPayloadMagic, StringComparison.Ordinal))
        {
            throw new MapFormatException($"{file.Path} has invalid navmesh payload magic '{magic}'.");
        }

        uint polygonCount = reader.ReadUInt32();
        uint connectionCount = reader.ReadUInt32();
        if (stream.Position != stream.Length)
        {
            throw new MapFormatException($"{file.Path} has {stream.Length - stream.Position} unread navmesh payload byte(s). Real navmesh payload parsing has not been implemented yet.");
        }

        MapTileKey key = new(file.Header.MapId, file.Header.TileX, file.Header.TileY);
        return new MapTileNavmeshData(key, file.Header.Build, polygonCount, connectionCount);
    }
}
