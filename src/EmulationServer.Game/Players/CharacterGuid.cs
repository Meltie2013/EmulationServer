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
// File: src/EmulationServer.Game/Players/CharacterGuid.cs
// Purpose: Contains character GUID code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Players;

// Type: CharacterGuid
// Purpose: Provides character GUID behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class CharacterGuid
{

    // Constant: Defines the high GUID item constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed high GUID item value used anywhere this rule or protocol value is needed.
    private const ushort HighGuidItem = 0x4000;
    // Constant: Defines the high GUID game object constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed high GUID game object value used anywhere this rule or protocol value is needed.
    private const ushort HighGuidGameObject = 0xF110;
    // Constant: Defines the high GUID creature constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed high GUID creature value used anywhere this rule or protocol value is needed.
    private const ushort HighGuidCreature = 0xF130;

    // Method: ToClientGuid
    // Purpose: Executes the to client GUID operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - lowGuid: Low GUID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the ulong value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterGuid so callers do not duplicate validation, protocol, or persistence rules.
    public static ulong ToClientGuid(uint lowGuid)
    {
        return ToPlayerGuid(lowGuid);
    }

    // Method: ToPlayerGuid
    // Purpose: Executes the to player GUID operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - lowGuid: Low GUID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the ulong value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterGuid so callers do not duplicate validation, protocol, or persistence rules.
    public static ulong ToPlayerGuid(uint lowGuid)
    {
        return lowGuid;
    }

    // Method: ToItemGuid
    // Purpose: Executes the to item GUID operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - lowGuid: Low GUID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the ulong value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterGuid so callers do not duplicate validation, protocol, or persistence rules.
    public static ulong ToItemGuid(uint lowGuid)
    {
        return lowGuid == 0 ? 0 : ((ulong)HighGuidItem << 48) | lowGuid;
    }

    // Method: ToGameObjectGuid
    // Purpose: Executes the to game object GUID operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - lowGuid: Low GUID identifier used to select the exact record, object, or runtime owner.
    // - entry: Entry value supplied by the caller for this operation.
    // Returns: Returns the ulong value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterGuid so callers do not duplicate validation, protocol, or persistence rules.
    public static ulong ToGameObjectGuid(uint lowGuid, uint entry)
    {
        return lowGuid == 0 || entry == 0
            ? 0
            : ((ulong)HighGuidGameObject << 48) | (((ulong)entry & 0xFFFFFFUL) << 24) | ((ulong)lowGuid & 0xFFFFFFUL);
    }

    // Method: ToCreatureGuid
    // Purpose: Executes the to creature GUID operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - lowGuid: Low GUID identifier used to select the exact record, object, or runtime owner.
    // - entry: Entry value supplied by the caller for this operation.
    // Returns: Returns the ulong value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterGuid so callers do not duplicate validation, protocol, or persistence rules.
    public static ulong ToCreatureGuid(uint lowGuid, uint entry)
    {
        return lowGuid == 0 || entry == 0
            ? 0
            : ((ulong)HighGuidCreature << 48) | (((ulong)entry & 0xFFFFFFUL) << 24) | ((ulong)lowGuid & 0xFFFFFFUL);
    }

    // Property: Gets or sets the game object high GUID value value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: game object high GUID value value exposed by the owning type.
    public static uint GameObjectHighGuidValue => HighGuidGameObject;

    // Property: Gets or sets the creature high GUID value value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: creature high GUID value value exposed by the owning type.
    public static uint CreatureHighGuidValue => HighGuidCreature;

    // Method: FromClientGuid
    // Purpose: Executes the from client GUID operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - clientGuid: Client GUID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterGuid so callers do not duplicate validation, protocol, or persistence rules.
    public static uint FromClientGuid(ulong clientGuid)
    {
        return (uint)(clientGuid & uint.MaxValue);
    }
}
