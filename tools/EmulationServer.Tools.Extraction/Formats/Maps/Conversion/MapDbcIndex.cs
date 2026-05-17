
using EmulationServer.Tools.Extraction.Formats.Dbc;

namespace EmulationServer.Tools.Extraction.Formats.Maps.Conversion;

public sealed class MapDbcIndex
{
    private readonly Dictionary<string, MapDbcEntry> _byDirectoryName;

    private MapDbcIndex(Dictionary<string, MapDbcEntry> byDirectoryName)
    {
        _byDirectoryName = byDirectoryName;
    }

    public static MapDbcIndex Load(string path)
    {
        DbcFile dbc = DbcFile.Load(path);
        Dictionary<string, MapDbcEntry> byDirectoryName = new(StringComparer.OrdinalIgnoreCase);

        foreach (DbcRecord record in dbc.EnumerateRecords())
        {
            uint id = record.GetUInt32(0);
            string directoryName = record.GetString(1);

            if (string.IsNullOrWhiteSpace(directoryName))
            {
                continue;
            }

            byDirectoryName[directoryName] = new MapDbcEntry(id, directoryName);
        }

        return new MapDbcIndex(byDirectoryName);
    }

    public bool TryGetByDirectoryName(string directoryName, out MapDbcEntry entry)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryName);

        if (_byDirectoryName.TryGetValue(directoryName, out MapDbcEntry? value))
        {
            entry = value;
            return true;
        }

        entry = null!;
        return false;
    }
}
