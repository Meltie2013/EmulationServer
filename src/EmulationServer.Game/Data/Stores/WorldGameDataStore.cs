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

using EmulationServer.Game.Data.Dbc;
using EmulationServer.Game.Data.Dbc.Characters;
using EmulationServer.Game.Data.Dbc.Chat;
using EmulationServer.Game.Data.Dbc.Factions;
using EmulationServer.Game.Data.Dbc.Items;
using EmulationServer.Game.Data.Dbc.Maps;
using EmulationServer.Game.Data.Dbc.Spells;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/EmulationServer.Game/Data/Stores/WorldGameDataStore.cs
  * Documents the WorldGameDataStore source file in the combined game data store construction and validation area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Data.Stores;

/**
  * Owns WorldServer DBC data that is needed for global validation and character flow.
  * It owns loaded data in memory and provides lookup access to other systems.
  */
public sealed class WorldGameDataStore
{
    private readonly Dictionary<string, DbcDataStore> _dbcStores;
    /**
      * Holds the private map data state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly MapDbcDataStore _mapData;
    /**
      * Holds the private character data state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly CharacterDbcDataStore _characterData;
    /**
      * Holds the private item data state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly ItemDbcDataStore _itemData;
    /**
      * Holds the private spell data state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly SpellDbcDataStore _spellData;
    /**
      * Holds the private faction data state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly FactionDbcDataStore _factionData;
    /**
      * Holds the private chat data state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly ChatChannelDbcDataStore _chatData;
    /**
      * Holds the private language data state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly LanguageDbcDataStore _languageData;

    /**
      * Initializes a new WorldGameDataStore instance with the dependencies required by the combined game data store construction and validation workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: dbcStores, mapData, characterData, itemData, spellData, factionData....
      */
    private WorldGameDataStore(
        Dictionary<string, DbcDataStore> dbcStores,
        MapDbcDataStore mapData,
        CharacterDbcDataStore characterData,
        ItemDbcDataStore itemData,
        SpellDbcDataStore spellData,
        FactionDbcDataStore factionData,
        ChatChannelDbcDataStore chatData,
        LanguageDbcDataStore languageData)
    {
        _dbcStores = dbcStores;
        _mapData = mapData;
        _characterData = characterData;
        _itemData = itemData;
        _spellData = spellData;
        _factionData = factionData;
        _chatData = chatData;
        _languageData = languageData;
    }

    /**
      * Gets or stores the empty value used by WorldGameDataStore.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public static WorldGameDataStore Empty { get; } = new(
        [],
        MapDbcDataStore.Empty,
        CharacterDbcDataStore.Empty,
        ItemDbcDataStore.Empty,
        SpellDbcDataStore.Empty,
        FactionDbcDataStore.Empty,
        ChatChannelDbcDataStore.Empty,
        LanguageDbcDataStore.Empty);

    public IReadOnlyDictionary<string, DbcDataStore> DbcStores => _dbcStores;

    /**
      * Gets typed map, area, trigger, continent, and overlay DBC data for character routing and map-service decisions.
      */
    public MapDbcDataStore MapData => _mapData;

    /**
      * Gets typed race, class, customization, and starter outfit DBC data.
      */
    public CharacterDbcDataStore CharacterData => _characterData;

    /**
      * Gets typed item class, subclass, display, set, and bag-family DBC data.
      */
    public ItemDbcDataStore ItemData => _itemData;

    /**
      * Gets typed spell, skill, range, duration, icon, and cast-time DBC data.
      */
    public SpellDbcDataStore SpellData => _spellData;

    /**
      * Gets typed faction and faction-template DBC data.
      */
    public FactionDbcDataStore FactionData => _factionData;

    /**
      * Gets typed chat-channel DBC data used for zone-scoped channel names and auto-join behavior.
      */
    public ChatChannelDbcDataStore ChatData => _chatData;

    /**
      * Gets typed language DBC data used to validate client chat language selections.
      */
    public LanguageDbcDataStore LanguageData => _languageData;

    /**
      * Attempts the operation without treating a normal failure as an exceptional condition.
      * The method is part of WorldGameDataStore and keeps this workflow isolated from the caller.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    public bool TryGetDbcStore(string fileName, out DbcDataStore store)
    {
        return _dbcStores.TryGetValue(fileName, out store!);
    }

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of WorldGameDataStore and keeps this workflow isolated from the caller.
      */
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
        SpellDbcDataStore spellData = SpellDbcDataStore.FromDbcStores(dbcStores, "WorldGameDataStore");
        FactionDbcDataStore factionData = FactionDbcDataStore.FromDbcStores(dbcStores, "WorldGameDataStore");
        ChatChannelDbcDataStore chatData = ChatChannelDbcDataStore.FromDbcStores(dbcStores, "WorldGameDataStore");
        LanguageDbcDataStore languageData = LanguageDbcDataStore.FromDbcStores(dbcStores, "WorldGameDataStore");

        Logger.Write(LogType.SUCCESS, $"World game data loaded: {dbcStores.Count} DBC file(s). Map tiles are owned by MapServer and InstanceServer.", "WorldGameDataStore");

        return new WorldGameDataStore(dbcStores, mapData, characterData, itemData, spellData, factionData, chatData, languageData);
    }
}
