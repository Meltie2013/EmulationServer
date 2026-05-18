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

/**
  * File overview: src/EmulationServer.Game/Data/Dbc/DbcHeader.cs
  * This file belongs to the DBC file loading, validation, and raw record access portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Game.Data.Dbc;

/**
  * Represents immutable dbc header data passed between parts of the server.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public sealed record DbcHeader(
    string Magic,
    int RecordCount,
    int FieldCount,
    int RecordSize,
    int StringBlockSize)
{
    public const string ExpectedMagic = "WDBC";

    /**
      * Gets or stores the uses four byte fields value used by DbcHeader.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public bool UsesFourByteFields => RecordSize == FieldCount * sizeof(uint);

    /**
      * Gets or stores the uses uniform compact fields value used by DbcHeader.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public bool UsesUniformCompactFields => TryGetUniformFieldSize(out _);

    /**
      * Attempts the operation without treating a normal failure as an exceptional condition.
      * The method is part of DbcHeader and keeps this workflow isolated from the caller.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
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
