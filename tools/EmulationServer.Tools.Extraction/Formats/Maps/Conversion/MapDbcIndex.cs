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
