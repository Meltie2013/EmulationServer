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
