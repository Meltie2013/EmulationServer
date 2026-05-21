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
  * File overview: tools/EmulationServer.Tools.Extraction/Formats/Vmaps/Conversion/VmapModel.cs
  * This file stores one converted WMO root model before it is written to disk.
  */

namespace EmulationServer.Tools.Extraction.Formats.Vmaps.Conversion;

/**
  * Represents a compact model assembled from a root WMO and its group files.
  */
public sealed class VmapModel
{
    /**
      * Creates a new compact model from parsed WMO groups.
      */
    public VmapModel(VmapModelName name, IReadOnlyList<WmoGroupGeometry> groups)
    {
        Name = name;
        Groups = groups;
        Bounds = BuildBounds(groups);
    }

    /**
      * Gets the model name and deterministic output key.
      */
    public VmapModelName Name { get; }

    /**
      * Gets all non-empty WMO group geometry included in this model.
      */
    public IReadOnlyList<WmoGroupGeometry> Groups { get; }

    /**
      * Gets the aggregate bounds for all converted groups.
      */
    public VmapBounds Bounds { get; }

    /**
      * Gets the total number of vertices across all groups.
      */
    public int VertexCount => Groups.Sum(static group => group.Vertices.Count);

    /**
      * Gets the total number of triangles across all groups.
      */
    public int TriangleCount => Groups.Sum(static group => group.TriangleCount);

    /**
      * Merges all group bounds into one model-level bounding box.
      */
    private static VmapBounds BuildBounds(IReadOnlyList<WmoGroupGeometry> groups)
    {
        VmapBounds bounds = VmapBounds.Empty;

        foreach (WmoGroupGeometry group in groups)
        {
            bounds = VmapBounds.Merge(bounds, group.Bounds);
        }

        return bounds;
    }
}
