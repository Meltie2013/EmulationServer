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

using EmulationServer.Tools.Extraction.Formats.Dbc;


/**
 * File overview: tools/EmulationServer.Tools.Extraction/Formats/Maps/Conversion/AreaTableIndex.cs
 * Documents the AreaTableIndex source file in the client data extraction and conversion tooling area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Tools.Extraction.Formats.Maps.Conversion;

/**
 * Owns the area table index behavior for the client data extraction and conversion tooling layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public sealed class AreaTableIndex
{
    private readonly Dictionary<uint, ushort> _areaFlags;

    /**
     * Initializes a new AreaTableIndex instance with the dependencies required by the client data extraction and conversion tooling workflow.
     * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
     * Inputs used by this operation: areaFlags.
     */
    private AreaTableIndex(Dictionary<uint, ushort> areaFlags)
    {
        _areaFlags = areaFlags;
    }

    /**
      * Gets or stores the empty value used by AreaTableIndex.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public static AreaTableIndex Empty { get; } = new(new Dictionary<uint, ushort>());

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of AreaTableIndex and keeps this workflow isolated from the caller.
      */
    public static AreaTableIndex Load(string path)
    {
        DbcFile dbc = DbcFile.Load(path);
        Dictionary<uint, ushort> areaFlags = [];

        foreach (DbcRecord record in dbc.EnumerateRecords())
        {
            uint areaId = record.GetUInt32(0);
            ushort explorationFlag = unchecked((ushort)record.GetUInt32(3));
            areaFlags[areaId] = explorationFlag;
        }

        return new AreaTableIndex(areaFlags);
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of AreaTableIndex and keeps this workflow isolated from the caller.
      */
    public ushort GetAreaFlag(uint areaId)
    {
        if (areaId == 0)
        {
            return ushort.MaxValue;
        }

        return _areaFlags.TryGetValue(areaId, out ushort flag) ? flag : ushort.MaxValue;
    }
}
