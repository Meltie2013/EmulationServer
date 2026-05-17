
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
