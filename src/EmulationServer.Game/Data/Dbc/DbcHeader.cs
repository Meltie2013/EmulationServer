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

namespace EmulationServer.Game.Data.Dbc;

public sealed record DbcHeader(
    string Magic,
    int RecordCount,
    int FieldCount,
    int RecordSize,
    int StringBlockSize)
{
    public const string ExpectedMagic = "WDBC";

    public bool UsesFourByteFields => RecordSize == FieldCount * sizeof(uint);

    public bool UsesUniformCompactFields => TryGetUniformFieldSize(out _);

    public bool TryGetUniformFieldSize(out int fieldSize)
    {
        fieldSize = 0;

        if (FieldCount <= 0 || RecordSize <= 0 || RecordSize % FieldCount != 0)
        {
            return false;
        }

        fieldSize = RecordSize / FieldCount;
        return fieldSize is sizeof(byte) or sizeof(ushort) or sizeof(uint);
    }
}
