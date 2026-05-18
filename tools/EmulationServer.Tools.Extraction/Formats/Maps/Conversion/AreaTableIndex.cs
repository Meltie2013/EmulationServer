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
  * This file belongs to the developer tooling for data extraction, validation, and diagnostics portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Tools.Extraction.Formats.Maps.Conversion;

/**
  * Represents the area table index component in the developer tooling for data extraction, validation, and diagnostics area.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public sealed class AreaTableIndex
{
    private readonly Dictionary<uint, ushort> _areaFlags;

    /**
      * Creates a new AreaTableIndex instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
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
