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
// File: src/EmulationServer.Game/Data/Dbc/DbcHeader.cs
// Purpose: Contains DBC header code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Data.Dbc;

// Type: DbcHeader
// Purpose: Represents DBC header data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - Magic: Magic value supplied by the caller for this operation.
// - RecordCount: Record count value supplied by the caller for this operation.
// - FieldCount: Field count value supplied by the caller for this operation.
// - RecordSize: Record size value supplied by the caller for this operation.
// - StringBlockSize: String block size value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record DbcHeader(
    string Magic,
    int RecordCount,
    int FieldCount,
    int RecordSize,
    int StringBlockSize)
{

    // Constant: Defines the expected magic constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed expected magic value used anywhere this rule or protocol value is needed.
    public const string ExpectedMagic = "WDBC";

    public bool UsesFourByteFields => RecordSize == FieldCount * sizeof(uint);

    // Method: TryGetUniformFieldSize
    // Purpose: Attempts to retrieve or parse try get uniform field size data without treating normal misses as failures.
    // Parameters:
    // - _: Value value supplied by the caller for this operation.
    // Returns: Returns a result indicating whether the requested value could be produced without throwing for normal failure cases.
    // Notes: This keeps the operation scoped to DbcHeader so callers do not duplicate validation, protocol, or persistence rules.
    public bool UsesUniformCompactFields => TryGetUniformFieldSize(out _);

    // Method: TryGetUniformFieldSize
    // Purpose: Attempts to retrieve or parse try get uniform field size data without treating normal misses as failures.
    // Parameters:
    // - fieldSize: Field size value supplied by the caller for this operation.
    // Returns: Returns true when try get uniform field size succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to DbcHeader so callers do not duplicate validation, protocol, or persistence rules.
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
