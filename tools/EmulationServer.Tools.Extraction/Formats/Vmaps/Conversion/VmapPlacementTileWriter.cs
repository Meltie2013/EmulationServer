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

/**
  * File overview: tools/EmulationServer.Tools.Extraction/Formats/Vmaps/Conversion/VmapPlacementTileWriter.cs
  * Documents the VmapPlacementTileWriter source file in the client data extraction and conversion tooling area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Tools.Extraction.Formats.Vmaps.Conversion;

/**
  * Writes WMO placement data for one map tile.
  */
public static class VmapPlacementTileWriter
{
    /**
      * Defines the constant value for version.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const uint Version = 1;

    /**
      * Writes one tile placement file.
      */
    public static void Write(string path, VmapPlacementTile tile, ushort build)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(tile);

        string? parentDirectory = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(parentDirectory))
        {
            Directory.CreateDirectory(parentDirectory);
        }

        byte[] payload = BuildPayload(tile, build);
        MapStoreBinary.WriteFile(
            path,
            MapStoreDataKind.Collision,
            build,
            tile.MapId,
            checked((byte)tile.TileX),
            checked((byte)tile.TileY),
            payload);
    }


    /**
      * Builds the vmap collision placement payload stored inside a collision mapstore file.
      */
    private static byte[] BuildPayload(VmapPlacementTile tile, ushort build)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true);

        MapStoreBinaryPrimitives.WriteAscii(writer, MapStorePayloadConstants.CollisionPayloadMagic);
        writer.Write(Version);
        writer.Write(build);
        writer.Write(tile.MapId);
        writer.Write(tile.TileX);
        writer.Write(tile.TileY);
        writer.Write(tile.Placements.Count);

        foreach (VmapPlacement placement in tile.Placements)
        {
            MapStoreBinaryPrimitives.WriteUtf8String(writer, placement.ModelName.Key);
            MapStoreBinaryPrimitives.WriteUtf8String(writer, placement.ModelName.NormalizedPath);
            writer.Write(placement.UniqueId);
            WriteVector(writer, placement.Position);
            WriteVector(writer, placement.Rotation);
            WriteBounds(writer, placement.Bounds);
            writer.Write(placement.Flags);
            writer.Write(placement.DoodadSet);
            writer.Write(placement.NameSet);
        }

        writer.Flush();
        return stream.ToArray();
    }

    /**
      * Writes an axis-aligned bounding box.
      */
    private static void WriteBounds(BinaryWriter writer, VmapBounds bounds)
    {
        WriteVector(writer, bounds.Minimum);
        WriteVector(writer, bounds.Maximum);
    }

    /**
      * Writes one three-component vector.
      */
    private static void WriteVector(BinaryWriter writer, VmapVector3 vector)
    {
        writer.Write(vector.X);
        writer.Write(vector.Y);
        writer.Write(vector.Z);
    }
}
