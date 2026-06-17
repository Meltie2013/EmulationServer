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
// File: tests/EmulationServer.Tests/Game/Players/CharacterGuidTests.cs
// Purpose: Contains character GUID tests code for the automated test and verification layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Game.Players;

namespace EmulationServer.Tests.Game.Players;

// Type: CharacterGuidTests
// Purpose: Provides character GUID tests behavior for the automated test and verification layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class CharacterGuidTests
{
    [Fact]
    // Method: ToGameObjectGuid_ShouldIncludeHighGuidEntryAndSpawnGuid
    // Purpose: Executes the to game object GUID should include high GUID entry and spawn GUID operation for the automated test and verification layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to CharacterGuidTests so callers do not duplicate validation, protocol, or persistence rules.
    public void ToGameObjectGuid_ShouldIncludeHighGuidEntryAndSpawnGuid()
    {
        ulong guid = CharacterGuid.ToGameObjectGuid(0x345678, 0x123456);

        Assert.Equal(0xF110UL, guid >> 48);
        Assert.Equal(0x123456UL, (guid >> 24) & 0xFFFFFFUL);
        Assert.Equal(0x345678UL, guid & 0xFFFFFFUL);
    }

    [Fact]
    // Method: ToGameObjectGuid_ShouldReturnZeroForMissingSpawnOrEntry
    // Purpose: Executes the to game object GUID should return zero for missing spawn or entry operation for the automated test and verification layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to CharacterGuidTests so callers do not duplicate validation, protocol, or persistence rules.
    public void ToGameObjectGuid_ShouldReturnZeroForMissingSpawnOrEntry()
    {
        Assert.Equal(0UL, CharacterGuid.ToGameObjectGuid(0, 1));
        Assert.Equal(0UL, CharacterGuid.ToGameObjectGuid(1, 0));
    }
}
