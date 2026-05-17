
using EmulationServer.Tools.Extraction.Formats.Dbc;

namespace EmulationServer.Tools.Extraction.Formats.Maps.Conversion;

public sealed class AreaTableIndex
{
    private readonly Dictionary<uint, ushort> _areaFlags;

    private AreaTableIndex(Dictionary<uint, ushort> areaFlags)
    {
        _areaFlags = areaFlags;
    }

    public static AreaTableIndex Empty { get; } = new(new Dictionary<uint, ushort>());

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

    public ushort GetAreaFlag(uint areaId)
    {
        if (areaId == 0)
        {
            return ushort.MaxValue;
        }

        return _areaFlags.TryGetValue(areaId, out ushort flag) ? flag : ushort.MaxValue;
    }
}
