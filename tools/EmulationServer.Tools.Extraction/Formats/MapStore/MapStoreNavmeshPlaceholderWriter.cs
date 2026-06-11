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

using System.Text;
using EmulationServer.Shared.Data.MapStore;

namespace EmulationServer.Tools.Extraction.Formats.MapStore;

/**
  * Writes empty navmesh mapstore files so runtime can validate required files before native navmesh generation exists.
  */
public static class MapStoreNavmeshPlaceholderWriter
{
    /**
      * Creates one empty navmesh file for every terrain tile currently present in the mapstore.
      */
    public static int WriteMissingNavmeshFiles(string mapStoreRootDirectory, ushort build, bool overwrite)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapStoreRootDirectory);

        string mapsDirectory = Path.Combine(mapStoreRootDirectory, MapStoreFileNames.MapsDirectoryName);
        if (!Directory.Exists(mapsDirectory))
        {
            return 0;
        }

        int written = 0;
        foreach (string mapDirectory in Directory.EnumerateDirectories(mapsDirectory).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            string mapFolderName = Path.GetFileName(mapDirectory);
            if (!MapStoreFileNames.TryParseMapDirectoryName(mapFolderName, out uint mapId))
            {
                continue;
            }

            string tilesDirectory = MapStoreFileNames.GetTilesDirectory(mapStoreRootDirectory, mapId);
            if (!Directory.Exists(tilesDirectory))
            {
                continue;
            }

            foreach (string terrainPath in Directory.EnumerateFiles(tilesDirectory, $"*{MapStoreFileNames.TerrainSuffix}", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                if (!MapStoreFileNames.TryParseTileFileName(Path.GetFileName(terrainPath), out byte tileX, out byte tileY, out MapStoreDataKind kind) || kind != MapStoreDataKind.Terrain)
                {
                    continue;
                }

                string navmeshPath = MapStoreFileNames.GetTileFilePath(mapStoreRootDirectory, mapId, tileX, tileY, MapStoreDataKind.Navmesh);
                if (!overwrite && File.Exists(navmeshPath))
                {
                    continue;
                }

                MapStoreBinary.WriteFile(navmeshPath, MapStoreDataKind.Navmesh, build, mapId, tileX, tileY, BuildEmptyNavmeshPayload());
                written++;
            }
        }

        MapStoreIndexWriter.RebuildIndexes(mapStoreRootDirectory, build);
        return written;
    }

    /**
      * Builds a minimal navmesh payload with zero polygons.
      */
    private static byte[] BuildEmptyNavmeshPayload()
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true);

        MapStoreBinaryPrimitives.WriteAscii(writer, MapStorePayloadConstants.NavmeshPayloadMagic);
        writer.Write((uint)0);
        writer.Write((uint)0);

        writer.Flush();
        return stream.ToArray();
    }
}
