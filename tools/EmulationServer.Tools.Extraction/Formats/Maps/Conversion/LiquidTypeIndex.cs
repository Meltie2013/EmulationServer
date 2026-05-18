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

namespace EmulationServer.Tools.Extraction.Formats.Maps.Conversion;

public sealed class LiquidTypeIndex
{
    public static LiquidTypeIndex Empty { get; } = new([]);

    private readonly Dictionary<ushort, ushort> _dbcLiquidCategoryById;

    private LiquidTypeIndex(Dictionary<ushort, ushort> dbcLiquidCategoryById)
    {
        _dbcLiquidCategoryById = dbcLiquidCategoryById;
    }

    public static LiquidTypeIndex Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        DbcFile dbcFile = DbcFile.Load(path);
        Dictionary<ushort, ushort> dbcLiquidCategoryById = [];

        foreach (DbcRecord record in dbcFile.EnumerateRecords())
        {
            ushort id = checked((ushort)record.GetUInt32(0));

            // MaNGOS reads field 3 from LiquidType.dbc as the liquid category/sound-bank value.
            ushort liquidCategory = checked((ushort)record.GetUInt32(3));

            dbcLiquidCategoryById[id] = liquidCategory;
        }

        return new LiquidTypeIndex(dbcLiquidCategoryById);
    }

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
