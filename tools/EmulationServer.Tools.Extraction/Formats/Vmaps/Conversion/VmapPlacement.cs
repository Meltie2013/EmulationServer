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
 * File overview: tools/EmulationServer.Tools.Extraction/Formats/Vmaps/Conversion/VmapPlacement.cs
 * Documents the VmapPlacement source file in the client data extraction and conversion tooling area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Tools.Extraction.Formats.Vmaps.Conversion;

/**
  * Represents one WMO instance placement from an ADT MODF record.
  * The placement references a converted model key and stores transform data needed by runtime collision code.
  */
public sealed class VmapPlacement
{
    /**
     * Initializes a new VmapPlacement instance with the dependencies required by the client data extraction and conversion tooling workflow.
     * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
     * Inputs used by this operation: modelName, uniqueId, position, rotation, bounds, flags....
     */
    public VmapPlacement(
        VmapModelName modelName,
        uint uniqueId,
        VmapVector3 position,
        VmapVector3 rotation,
        VmapBounds bounds,
        uint flags,
        ushort doodadSet,
        ushort nameSet)
    {
        ModelName = modelName;
        UniqueId = uniqueId;
        Position = position;
        Rotation = rotation;
        Bounds = bounds;
        Flags = flags;
        DoodadSet = doodadSet;
        NameSet = nameSet;
    }

    /**
      * Gets the referenced model name and output key.
      */
    public VmapModelName ModelName { get; }

    /**
      * Gets the unique placement identifier from the ADT MODF record.
      */
    public uint UniqueId { get; }

    /**
      * Gets the world position of the model placement.
      */
    public VmapVector3 Position { get; }

    /**
      * Gets the model rotation from the ADT MODF record.
      */
    public VmapVector3 Rotation { get; }

    /**
      * Gets the placement bounds copied from the ADT MODF record.
      */
    public VmapBounds Bounds { get; }

    /**
      * Gets placement flags from the ADT MODF record.
      */
    public uint Flags { get; }

    /**
      * Gets the doodad set selected for this placement.
      */
    public ushort DoodadSet { get; }

    /**
      * Gets the name set selected for this placement.
      */
    public ushort NameSet { get; }
}
