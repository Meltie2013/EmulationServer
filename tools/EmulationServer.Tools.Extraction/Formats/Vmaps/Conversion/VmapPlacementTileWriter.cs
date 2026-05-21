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

/**
  * File overview: tools/EmulationServer.Tools.Extraction/Formats/Vmaps/Conversion/VmapPlacementTileWriter.cs
  * This file writes compact vmap tile placement files generated from ADT MODF records.
  */

namespace EmulationServer.Tools.Extraction.Formats.Vmaps.Conversion;

/**
  * Writes WMO placement data for one map tile.
  */
public static class VmapPlacementTileWriter
{
    private const string Magic = "ESVTIL1";
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

        using FileStream stream = File.Create(path);
        using BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: false);

        WriteMagic(writer, Magic);
        writer.Write(Version);
        writer.Write(build);
        writer.Write(tile.MapId);
        writer.Write(tile.TileX);
        writer.Write(tile.TileY);
        writer.Write(tile.Placements.Count);

        foreach (VmapPlacement placement in tile.Placements)
        {
            WriteString(writer, placement.ModelName.Key);
            WriteString(writer, placement.ModelName.NormalizedPath);
            writer.Write(placement.UniqueId);
            WriteVector(writer, placement.Position);
            WriteVector(writer, placement.Rotation);
            WriteBounds(writer, placement.Bounds);
            writer.Write(placement.Flags);
            writer.Write(placement.DoodadSet);
            writer.Write(placement.NameSet);
        }
    }

    /**
      * Writes a fixed-size ASCII magic value.
      */
    private static void WriteMagic(BinaryWriter writer, string value)
    {
        writer.Write(Encoding.ASCII.GetBytes(value));
    }

    /**
      * Writes a length-prefixed UTF-8 string.
      */
    private static void WriteString(BinaryWriter writer, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        writer.Write(bytes.Length);
        writer.Write(bytes);
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
