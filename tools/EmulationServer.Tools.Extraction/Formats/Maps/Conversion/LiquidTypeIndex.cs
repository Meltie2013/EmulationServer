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
  * File overview: tools/EmulationServer.Tools.Extraction/Formats/Maps/Conversion/LiquidTypeIndex.cs
  * Documents the LiquidTypeIndex source file in the client data extraction and conversion tooling area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Tools.Extraction.Formats.Maps.Conversion;

/**
  * Owns the liquid type index behavior for the client data extraction and conversion tooling layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class LiquidTypeIndex
{
    /**
      * Gets or stores the empty value used by LiquidTypeIndex.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public static LiquidTypeIndex Empty { get; } = new([]);

    private readonly Dictionary<ushort, ushort> _dbcLiquidCategoryById;

    /**
      * Initializes a new LiquidTypeIndex instance with the dependencies required by the client data extraction and conversion tooling workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: dbcLiquidCategoryById.
      */
    private LiquidTypeIndex(Dictionary<ushort, ushort> dbcLiquidCategoryById)
    {
        _dbcLiquidCategoryById = dbcLiquidCategoryById;
    }

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of LiquidTypeIndex and keeps this workflow isolated from the caller.
      */
    public static LiquidTypeIndex Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        DbcFile dbcFile = DbcFile.Load(path);
        Dictionary<ushort, ushort> dbcLiquidCategoryById = [];

        foreach (DbcRecord record in dbcFile.EnumerateRecords())
        {
            ushort id = checked((ushort)record.GetUInt32(0));

            // The map converter reads field 3 from LiquidType.dbc as the liquid category/sound-bank value.
            ushort liquidCategory = checked((ushort)record.GetUInt32(3));

            dbcLiquidCategoryById[id] = liquidCategory;
        }

        return new LiquidTypeIndex(dbcLiquidCategoryById);
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of LiquidTypeIndex and keeps this workflow isolated from the caller.
      */
    public byte GetMapLiquidFlags(ushort liquidTypeId)
    {
        if (liquidTypeId == 0)
        {
            return MapFormatConstants.MapLiquidTypeNoWater;
        }

        if (!_dbcLiquidCategoryById.TryGetValue(liquidTypeId, out ushort liquidCategory))
        {
            return MapFormatConstants.MapLiquidTypeNoWater;
        }

        return liquidCategory switch
        {
            0 => MapFormatConstants.MapLiquidTypeWater,
            1 => MapFormatConstants.MapLiquidTypeOcean,
            2 => MapFormatConstants.MapLiquidTypeMagma,
            3 => MapFormatConstants.MapLiquidTypeSlime,
            _ => MapFormatConstants.MapLiquidTypeNoWater,
        };
    }
}
