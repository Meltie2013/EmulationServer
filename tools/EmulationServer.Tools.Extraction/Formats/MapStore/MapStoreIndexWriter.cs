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

namespace EmulationServer.Tools.Extraction.Formats.MapStore;

/**
  * Builds one map.index.bin file per map by scanning generated mapstore tile files.
  */
public static class MapStoreIndexWriter
{
    /**
      * Rebuilds every map.index.bin file under the supplied mapstore root.
      */
    public static void RebuildIndexes(string mapStoreRootDirectory, ushort build)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapStoreRootDirectory);

        string mapsDirectory = Path.Combine(mapStoreRootDirectory, MapStoreFileNames.MapsDirectoryName);
        if (!Directory.Exists(mapsDirectory))
        {
            return;
        }

        foreach (string mapDirectory in Directory.EnumerateDirectories(mapsDirectory).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            string mapFolderName = Path.GetFileName(mapDirectory);
            if (!MapStoreFileNames.TryParseMapDirectoryName(mapFolderName, out uint mapId))
            {
                continue;
            }

            RebuildIndex(mapStoreRootDirectory, mapId, build);
        }
    }

    /**
      * Rebuilds one map.index.bin file for the supplied map id.
      */
    public static void RebuildIndex(string mapStoreRootDirectory, uint mapId, ushort build)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapStoreRootDirectory);

        string tilesDirectory = MapStoreFileNames.GetTilesDirectory(mapStoreRootDirectory, mapId);
        if (!Directory.Exists(tilesDirectory))
        {
            return;
        }

        SortedDictionary<(byte TileX, byte TileY), MapStoreTileDataFlags> records = [];
        foreach (string path in Directory.EnumerateFiles(tilesDirectory, "*.bin", SearchOption.TopDirectoryOnly))
        {
            if (!MapStoreFileNames.TryParseTileFileName(Path.GetFileName(path), out byte tileX, out byte tileY, out MapStoreDataKind kind))
            {
                continue;
            }

            records.TryGetValue((tileX, tileY), out MapStoreTileDataFlags flags);
            records[(tileX, tileY)] = flags | MapStoreFormat.GetTileDataFlag(kind);
        }

        string indexPath = MapStoreFileNames.GetIndexPath(mapStoreRootDirectory, mapId);
        Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);

        using FileStream stream = File.Open(indexPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        using BinaryWriter writer = new(stream);

        MapStoreBinaryPrimitives.WriteFourCC(writer, MapStoreFormat.IndexMagic);
        writer.Write(MapStoreFormat.CurrentVersion);
        writer.Write(build);
        writer.Write(mapId);
        writer.Write(records.Count);

        foreach (KeyValuePair<(byte TileX, byte TileY), MapStoreTileDataFlags> record in records)
        {
            writer.Write(record.Key.TileX);
            writer.Write(record.Key.TileY);
            writer.Write((byte)record.Value);
            writer.Write((byte)0);
        }
    }
}
