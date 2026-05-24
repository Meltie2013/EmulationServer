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

/**
  * File overview: tools/EmulationServer.Tools.Extraction/Formats/Vmaps/Conversion/VmapBounds.cs
  * Documents the VmapBounds source file in the client data extraction and conversion tooling area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Tools.Extraction.Formats.Vmaps.Conversion;

/**
  * Stores an axis-aligned bounding box using minimum and maximum points.
  * Bounds are written into converted vmap files so runtime code can reject obvious misses before doing detailed triangle tests.
  * Positional fields carried by this record: Minimum, Maximum.
  */
public readonly record struct VmapBounds(VmapVector3 Minimum, VmapVector3 Maximum)
{
    /**
      * Represents an empty bounds value for source records that do not provide valid bounds.
      */
    public static VmapBounds Empty { get; } = new(VmapVector3.Zero, VmapVector3.Zero);

    /**
      * Builds a bounding box from a vertex collection.
      * This is used for group geometry when the WMO group header does not contain a usable box.
      */
    public static VmapBounds FromVertices(IReadOnlyList<VmapVector3> vertices)
    {
        if (vertices.Count == 0)
        {
            return Empty;
        }

        float minX = vertices[0].X;
        float minY = vertices[0].Y;
        float minZ = vertices[0].Z;
        float maxX = vertices[0].X;
        float maxY = vertices[0].Y;
        float maxZ = vertices[0].Z;

        foreach (VmapVector3 vertex in vertices)
        {
            minX = MathF.Min(minX, vertex.X);
            minY = MathF.Min(minY, vertex.Y);
            minZ = MathF.Min(minZ, vertex.Z);
            maxX = MathF.Max(maxX, vertex.X);
            maxY = MathF.Max(maxY, vertex.Y);
            maxZ = MathF.Max(maxZ, vertex.Z);
        }

        return new VmapBounds(new VmapVector3(minX, minY, minZ), new VmapVector3(maxX, maxY, maxZ));
    }

    /**
      * Merges two bounding boxes into a single box that contains both inputs.
      * Empty values are tolerated so callers can build aggregate bounds incrementally.
      */
    public static VmapBounds Merge(VmapBounds first, VmapBounds second)
    {
        if (first == Empty)
        {
            return second;
        }

        if (second == Empty)
        {
            return first;
        }

        return new VmapBounds(
            new VmapVector3(
                MathF.Min(first.Minimum.X, second.Minimum.X),
                MathF.Min(first.Minimum.Y, second.Minimum.Y),
                MathF.Min(first.Minimum.Z, second.Minimum.Z)),
            new VmapVector3(
                MathF.Max(first.Maximum.X, second.Maximum.X),
                MathF.Max(first.Maximum.Y, second.Maximum.Y),
                MathF.Max(first.Maximum.Z, second.Maximum.Z)));
    }
}
