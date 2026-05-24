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
  * File overview: tools/EmulationServer.Tools.Extraction/Formats/Vmaps/Conversion/VmapModelWriter.cs
  * Documents the VmapModelWriter source file in the client data extraction and conversion tooling area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Tools.Extraction.Formats.Vmaps.Conversion;

/**
  * Owns the vmap model writer behavior for the client data extraction and conversion tooling layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public static class VmapModelWriter
{
    /**
      * Defines the constant value for magic.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const string Magic = "ESVMOD1";
    /**
      * Defines the constant value for version.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const uint Version = 1;

    /**
      * Writes one compact model file.
      */
    public static void Write(string path, VmapModel model, ushort build)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(model);

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
        WriteString(writer, model.Name.NormalizedPath);
        WriteBounds(writer, model.Bounds);
        writer.Write(model.Groups.Count);
        writer.Write(model.VertexCount);
        writer.Write(model.TriangleCount);

        foreach (WmoGroupGeometry group in model.Groups)
        {
            writer.Write(group.GroupIndex);
            writer.Write(group.Flags);
            WriteBounds(writer, group.Bounds);
            writer.Write(group.Vertices.Count);
            writer.Write(group.Indices.Count);

            foreach (VmapVector3 vertex in group.Vertices)
            {
                WriteVector(writer, vertex);
            }

            foreach (int index in group.Indices)
            {
                writer.Write(index);
            }
        }
    }

    /**
      * Writes a fixed-size ASCII magic value.
      */
    private static void WriteMagic(BinaryWriter writer, string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        writer.Write(bytes);
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
