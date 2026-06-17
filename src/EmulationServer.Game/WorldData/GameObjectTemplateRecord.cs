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
// File: src/EmulationServer.Game/WorldData/GameObjectTemplateRecord.cs
// Purpose: Contains game object template record code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.WorldData;

// Type: GameObjectTemplateRecord
// Purpose: Represents game object template record data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - Entry: Entry value supplied by the caller for this operation.
// - Type: Type value supplied by the caller for this operation.
// - DisplayId: Display ID identifier used to select the exact record, object, or runtime owner.
// - Name: Name value supplied by the caller for this operation.
// - Faction: Faction value supplied by the caller for this operation.
// - Flags: Flags value supplied by the caller for this operation.
// - Size: Size value supplied by the caller for this operation.
// - DataFields: Data fields value supplied by the caller for this operation.
// - MinGold: Min gold value supplied by the caller for this operation.
// - MaxGold: Max gold value supplied by the caller for this operation.
// - ScriptName: Script name value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record GameObjectTemplateRecord(
    uint Entry,
    byte Type,
    uint DisplayId,
    string Name,
    ushort Faction,
    uint Flags,
    float Size,
    IReadOnlyList<uint> DataFields,
    uint MinGold,
    uint MaxGold,
    string ScriptName)
{

    // Constant: Defines the data field count constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed data field count value used anywhere this rule or protocol value is needed.
    public const int DataFieldCount = 24;

    // Method: GetDataField
    // Purpose: Retrieves get data field data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - index: Index value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to GameObjectTemplateRecord so callers do not duplicate validation, protocol, or persistence rules.
    public uint GetDataField(int index)
    {
        return index >= 0 && index < DataFields.Count ? DataFields[index] : 0u;
    }
}
