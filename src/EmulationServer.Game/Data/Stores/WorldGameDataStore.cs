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
// File: src/EmulationServer.Game/Data/Stores/WorldGameDataStore.cs
// Purpose: Contains world game data store code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Game.Data.Dbc;
using EmulationServer.Game.Data.Dbc.Characters;
using EmulationServer.Game.Data.Dbc.Chat;
using EmulationServer.Game.Data.Dbc.Creatures;
using EmulationServer.Game.Data.Dbc.Factions;
using EmulationServer.Game.Data.Dbc.Items;
using EmulationServer.Game.Data.Dbc.Maps;
using EmulationServer.Game.Data.Dbc.Spells;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Game.Data.Stores;

// Type: WorldGameDataStore
// Purpose: Provides world game data store behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class WorldGameDataStore
{
    // Field: Stores the string state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current string backing value maintained by the owning type.
    private readonly Dictionary<string, DbcDataStore> _dbcStores;

    // Field: Stores the map data state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current map data backing value maintained by the owning type.
    private readonly MapDbcDataStore _mapData;

    // Field: Stores the character data state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current character data backing value maintained by the owning type.
    private readonly CharacterDbcDataStore _characterData;

    // Field: Stores the item data state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current item data backing value maintained by the owning type.
    private readonly ItemDbcDataStore _itemData;
    // Field: Stores the creature data state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current creature data backing value maintained by the owning type.
    private readonly CreatureDbcDataStore _creatureData;

    // Field: Stores the spell data state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current spell data backing value maintained by the owning type.
    private readonly SpellDbcDataStore _spellData;

    // Field: Stores the faction data state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current faction data backing value maintained by the owning type.
    private readonly FactionDbcDataStore _factionData;

    // Field: Stores the chat data state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current chat data backing value maintained by the owning type.
    private readonly ChatChannelDbcDataStore _chatData;

    // Field: Stores the language data state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current language data backing value maintained by the owning type.
    private readonly LanguageDbcDataStore _languageData;

    // Constructor: WorldGameDataStore
    // Purpose: Initializes a new WorldGameDataStore instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - dbcStores: Dbc stores value supplied by the caller for this operation.
    // - mapData: Map data value supplied by the caller for this operation.
    // - characterData: Character data value supplied by the caller for this operation.
    // - itemData: Item data value supplied by the caller for this operation.
    // - creatureData: Creature data value supplied by the caller for this operation.
    // - spellData: Spell data value supplied by the caller for this operation.
    // - factionData: Faction data value supplied by the caller for this operation.
    // - chatData: Chat data value supplied by the caller for this operation.
    // - languageData: Language data value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldGameDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private WorldGameDataStore(
        Dictionary<string, DbcDataStore> dbcStores,
        MapDbcDataStore mapData,
        CharacterDbcDataStore characterData,
        ItemDbcDataStore itemData,
        CreatureDbcDataStore creatureData,
        SpellDbcDataStore spellData,
        FactionDbcDataStore factionData,
        ChatChannelDbcDataStore chatData,
        LanguageDbcDataStore languageData)
    {
        _dbcStores = dbcStores;
        _mapData = mapData;
        _characterData = characterData;
        _itemData = itemData;
        _creatureData = creatureData;
        _spellData = spellData;
        _factionData = factionData;
        _chatData = chatData;
        _languageData = languageData;
    }

    public static WorldGameDataStore Empty { get; } = new(
        [],
        MapDbcDataStore.Empty,
        CharacterDbcDataStore.Empty,
        ItemDbcDataStore.Empty,
        CreatureDbcDataStore.Empty,
        SpellDbcDataStore.Empty,
        FactionDbcDataStore.Empty,
        ChatChannelDbcDataStore.Empty,
        LanguageDbcDataStore.Empty);

    // Property: Gets or sets the string value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: string value exposed by the owning type.
    public IReadOnlyDictionary<string, DbcDataStore> DbcStores => _dbcStores;

    // Property: Gets or sets the map data value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: map data value exposed by the owning type.
    public MapDbcDataStore MapData => _mapData;

    // Property: Gets or sets the character data value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: character data value exposed by the owning type.
    public CharacterDbcDataStore CharacterData => _characterData;

    // Property: Gets or sets the item data value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: item data value exposed by the owning type.
    public ItemDbcDataStore ItemData => _itemData;

    // Property: Gets or sets the creature data value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: creature data value exposed by the owning type.
    public CreatureDbcDataStore CreatureData => _creatureData;

    // Property: Gets or sets the spell data value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: spell data value exposed by the owning type.
    public SpellDbcDataStore SpellData => _spellData;

    // Property: Gets or sets the faction data value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: faction data value exposed by the owning type.
    public FactionDbcDataStore FactionData => _factionData;

    // Property: Gets or sets the chat data value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: chat data value exposed by the owning type.
    public ChatChannelDbcDataStore ChatData => _chatData;

    // Property: Gets or sets the language data value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: language data value exposed by the owning type.
    public LanguageDbcDataStore LanguageData => _languageData;

    // Method: TryGetDbcStore
    // Purpose: Attempts to retrieve or parse try get DBC store data without treating normal misses as failures.
    // Parameters:
    // - fileName: File name value supplied by the caller for this operation.
    // - store: Store value supplied by the caller for this operation.
    // Returns: Returns true when try get DBC store succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldGameDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetDbcStore(string fileName, out DbcDataStore store)
    {
        return _dbcStores.TryGetValue(fileName, out store!);
    }

    // Method: Load
    // Purpose: Retrieves load data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - dataDirectory: Data directory value supplied by the caller for this operation.
    // - dbcDirectory: Dbc directory value supplied by the caller for this operation.
    // - requiredDbcFiles: Required DBC files value supplied by the caller for this operation.
    // Returns: Returns the world game data store value produced by this operation.
    // Notes: This keeps the operation scoped to WorldGameDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public static WorldGameDataStore Load(
        string dataDirectory,
        string dbcDirectory,
        IEnumerable<string> requiredDbcFiles)
    {
        string fullDataDirectory = Path.GetFullPath(dataDirectory);
        string fullDbcDirectory = GameDataPathResolver.ResolveDirectory(fullDataDirectory, dbcDirectory);

        Dictionary<string, DbcDataStore> dbcStores = DbcStoreLoader.LoadRequiredStores(
            fullDbcDirectory,
            requiredDbcFiles,
            "WorldGameDataStore");

        MapDbcDataStore mapData = MapDbcDataStore.FromDbcStores(dbcStores, "WorldGameDataStore");
        CharacterDbcDataStore characterData = CharacterDbcDataStore.FromDbcStores(dbcStores, "WorldGameDataStore");
        ItemDbcDataStore itemData = ItemDbcDataStore.FromDbcStores(dbcStores, "WorldGameDataStore");
        CreatureDbcDataStore creatureData = CreatureDbcDataStore.FromDbcStores(dbcStores, "WorldGameDataStore");
        SpellDbcDataStore spellData = SpellDbcDataStore.FromDbcStores(dbcStores, "WorldGameDataStore");
        FactionDbcDataStore factionData = FactionDbcDataStore.FromDbcStores(dbcStores, "WorldGameDataStore");
        ChatChannelDbcDataStore chatData = ChatChannelDbcDataStore.FromDbcStores(dbcStores, "WorldGameDataStore");
        LanguageDbcDataStore languageData = LanguageDbcDataStore.FromDbcStores(dbcStores, "WorldGameDataStore");

        Logger.Write(
            LogType.SUCCESS,
            string.Join(Environment.NewLine,
                "World game data loaded:",
                $"  DBC stores: {dbcStores.Count}",
                "  Map tile owners: MapServer/InstanceServer"),
            "WorldGameDataStore");

        return new WorldGameDataStore(dbcStores, mapData, characterData, itemData, creatureData, spellData, factionData, chatData, languageData);
    }
}
