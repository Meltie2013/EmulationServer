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
// File: tests/EmulationServer.Tests/Game/WorldData/GameObjectWorldTemplateDataStoreTests.cs
// Purpose: Contains game object world template data store tests code for the automated test and verification layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Game.WorldData;

namespace EmulationServer.Tests.Game.WorldData;

// Type: GameObjectWorldTemplateDataStoreTests
// Purpose: Provides game object world template data store tests behavior for the automated test and verification layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class GameObjectWorldTemplateDataStoreTests
{
    [Fact]
    // Method: GameObjectSpawns_AreIndexedByMapZoneAndArea
    // Purpose: Executes the game object spawns are indexed by map zone and area operation for the automated test and verification layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to GameObjectWorldTemplateDataStoreTests so callers do not duplicate validation, protocol, or persistence rules.
    public void GameObjectSpawns_AreIndexedByMapZoneAndArea()
    {
        WorldTemplateDataStore store = CreateStore(
            [CreateTemplate(100)],
            [
                CreateSpawn(1, 100, 0, 12, 120),
                CreateSpawn(2, 100, 0, 12, 121),
                CreateSpawn(3, 100, 1, 13, 130),
            ]);

        Assert.Equal(3, store.GameObjectSpawnCount);
        Assert.Equal(2, store.GetGameObjectSpawnsForMap(0).Count);
        Assert.Equal(2, store.GetGameObjectSpawnsForZone(0, 12).Count);
        Assert.Single(store.GetGameObjectSpawnsForArea(0, 121));
        Assert.True(store.TryGetGameObjectTemplate(100, out GameObjectTemplateRecord? template));
        Assert.Equal("Test GameObject 100", template.Name);
    }

    [Fact]
    // Method: GameObjectSpawns_SkipRowsWithoutDisplayZoneOrArea
    // Purpose: Executes the game object spawns skip rows without display zone or area operation for the automated test and verification layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to GameObjectWorldTemplateDataStoreTests so callers do not duplicate validation, protocol, or persistence rules.
    public void GameObjectSpawns_SkipRowsWithoutDisplayZoneOrArea()
    {
        WorldTemplateDataStore store = CreateStore(
            [CreateTemplate(100), CreateTemplate(200) with { DisplayId = 0 }],
            [
                CreateSpawn(1, 100, 0, 12, 120),
                CreateSpawn(2, 100, 0, 0, 120),
                CreateSpawn(3, 100, 0, 12, 0),
                CreateSpawn(4, 200, 0, 12, 120),
            ]);

        Assert.Equal(1, store.GameObjectTemplateCount);
        Assert.Equal(1, store.GameObjectSpawnCount);
        Assert.True(store.TryGetGameObjectSpawn(1, out _));
        Assert.False(store.TryGetGameObjectSpawn(2, out _));
        Assert.False(store.TryGetGameObjectSpawn(3, out _));
        Assert.False(store.TryGetGameObjectSpawn(4, out _));
    }

    [Fact]
    // Method: WithGameObjectSpawns_RebuildsZoneAndAreaIndexes
    // Purpose: Executes the with game object spawns rebuilds zone and area indexes operation for the automated test and verification layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to GameObjectWorldTemplateDataStoreTests so callers do not duplicate validation, protocol, or persistence rules.
    public void WithGameObjectSpawns_RebuildsZoneAndAreaIndexes()
    {
        WorldTemplateDataStore store = CreateStore(
            [CreateTemplate(100)],
            [CreateSpawn(1, 100, 0, 0, 0)]);

        WorldTemplateDataStore updated = store.WithGameObjectSpawns(
            [CreateSpawn(1, 100, 0, 85, 512)]);

        Assert.Empty(updated.GetGameObjectSpawnsForZone(0, 0));
        Assert.Empty(updated.GetGameObjectSpawnsForArea(0, 0));
        Assert.Single(updated.GetGameObjectSpawnsForZone(0, 85));
        Assert.Single(updated.GetGameObjectSpawnsForArea(0, 512));
    }

    [Fact]
    // Method: WithGameObjectDataForMap_ReplacesOnlyRequestedMapSpawns
    // Purpose: Executes the with game object data for map replaces only requested map spawns operation for the automated test and verification layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to GameObjectWorldTemplateDataStoreTests so callers do not duplicate validation, protocol, or persistence rules.
    public void WithGameObjectDataForMap_ReplacesOnlyRequestedMapSpawns()
    {
        WorldTemplateDataStore store = CreateStore(
            [CreateTemplate(100)],
            [
                CreateSpawn(1, 100, 0, 12, 120),
                CreateSpawn(2, 100, 1, 13, 130),
            ]);

        WorldTemplateDataStore updated = store.WithGameObjectDataForMap(
            0,
            [CreateTemplate(100), CreateTemplate(200)],
            [CreateSpawn(3, 200, 0, 14, 140)]);

        Assert.Single(updated.GetGameObjectSpawnsForMap(0));
        Assert.Single(updated.GetGameObjectSpawnsForMap(1));
        Assert.Equal(2, updated.GameObjectTemplateCount);
        Assert.True(updated.TryGetGameObjectSpawn(3, out GameObjectSpawnRecord? spawn));
        Assert.Equal((uint)200, spawn.Entry);
        Assert.False(updated.TryGetGameObjectSpawn(1, out _));
    }

    // Method: CreateStore
    // Purpose: Applies create store changes for the automated test and verification layer.
    // Parameters:
    // - templates: Templates value supplied by the caller for this operation.
    // - spawns: Spawns value supplied by the caller for this operation.
    // Returns: Returns the world template data store value produced by this operation.
    // Notes: This keeps the operation scoped to GameObjectWorldTemplateDataStoreTests so callers do not duplicate validation, protocol, or persistence rules.
    private static WorldTemplateDataStore CreateStore(
        IEnumerable<GameObjectTemplateRecord> templates,
        IEnumerable<GameObjectSpawnRecord> spawns)
    {
        return new WorldTemplateDataStore(
            Array.Empty<PlayerCreateInfoRecord>(),
            Array.Empty<ItemTemplateRecord>(),
            Array.Empty<PlayerLevelStatsRecord>(),
            Array.Empty<PlayerClassLevelStatsRecord>(),
            Array.Empty<PlayerLevelExperienceRecord>(),
            Array.Empty<PlayerCreateActionRecord>(),
            Array.Empty<PlayerCreateItemRecord>(),
            Array.Empty<PlayerCreateSpellRecord>(),
            templates,
            spawns);
    }

    // Method: CreateTemplate
    // Purpose: Applies create template changes for the automated test and verification layer.
    // Parameters:
    // - entry: Entry value supplied by the caller for this operation.
    // Returns: Returns the game object template record value produced by this operation.
    // Notes: This keeps the operation scoped to GameObjectWorldTemplateDataStoreTests so callers do not duplicate validation, protocol, or persistence rules.
    private static GameObjectTemplateRecord CreateTemplate(uint entry)
    {
        return new GameObjectTemplateRecord(
            entry,
            3,
            10,
            $"Test GameObject {entry}",
            0,
            0,
            1.0f,
            Enumerable.Repeat(0u, GameObjectTemplateRecord.DataFieldCount).ToArray(),
            0,
            0,
            string.Empty);
    }

    // Method: CreateSpawn
    // Purpose: Applies create spawn changes for the automated test and verification layer.
    // Parameters:
    // - guid: Guid identifier used to select the exact record, object, or runtime owner.
    // - entry: Entry value supplied by the caller for this operation.
    // - map: Map value supplied by the caller for this operation.
    // - zone: Zone value supplied by the caller for this operation.
    // - area: Area value supplied by the caller for this operation.
    // Returns: Returns the game object spawn record value produced by this operation.
    // Notes: This keeps the operation scoped to GameObjectWorldTemplateDataStoreTests so callers do not duplicate validation, protocol, or persistence rules.
    private static GameObjectSpawnRecord CreateSpawn(uint guid, uint entry, ushort map, uint zone, uint area)
    {
        return new GameObjectSpawnRecord(
            guid,
            entry,
            map,
            zone,
            area,
            1.0f,
            2.0f,
            3.0f,
            4.0f,
            0.0f,
            0.0f,
            0.0f,
            1.0f,
            120,
            100,
            1);
    }
}
