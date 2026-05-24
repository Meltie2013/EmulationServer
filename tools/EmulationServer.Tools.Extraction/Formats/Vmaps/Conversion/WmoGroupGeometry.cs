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
 * File overview: tools/EmulationServer.Tools.Extraction/Formats/Vmaps/Conversion/WmoGroupGeometry.cs
 * Documents the WmoGroupGeometry source file in the client data extraction and conversion tooling area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Tools.Extraction.Formats.Vmaps.Conversion;

/**
  * Represents render/collision geometry extracted from a WMO group file.
  * WMO groups contain the actual vertex and triangle index data used by vmap line-of-sight and collision checks.
  */
public sealed class WmoGroupGeometry
{
    /**
     * Initializes a new WmoGroupGeometry instance with the dependencies required by the client data extraction and conversion tooling workflow.
     * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
     * Inputs used by this operation: groupIndex, flags, bounds, vertices, indices.
     */
    public WmoGroupGeometry(int groupIndex, uint flags, VmapBounds bounds, IReadOnlyList<VmapVector3> vertices, IReadOnlyList<int> indices)
    {
        GroupIndex = groupIndex;
        Flags = flags;
        Bounds = bounds;
        Vertices = vertices;
        Indices = indices;
    }

    /**
      * Gets the WMO group index from the root WMO group list.
      */
    public int GroupIndex { get; }

    /**
      * Gets the WMO group flags copied from the MOGP header when available.
      */
    public uint Flags { get; }

    /**
      * Gets the group bounds used for broad-phase collision rejection.
      */
    public VmapBounds Bounds { get; }

    /**
      * Gets the converted group vertices.
      */
    public IReadOnlyList<VmapVector3> Vertices { get; }

    /**
      * Gets triangle indices into the Vertices collection.
      */
    public IReadOnlyList<int> Indices { get; }

    /**
      * Gets the number of complete triangles represented by the group index list.
      */
    public int TriangleCount => Indices.Count / 3;
}
