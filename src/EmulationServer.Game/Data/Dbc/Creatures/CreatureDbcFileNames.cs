//
// Copyright (C) 2026 Emulation Server Project
//

namespace EmulationServer.Game.Data.Dbc.Creatures;

/**
  * Defines creature-related DBC filenames needed by creature/NPC template validation and runtime metadata.
  */
public static class CreatureDbcFileNames
{
    public const string CreatureDisplayInfo = "CreatureDisplayInfo.dbc";
    public const string CreatureDisplayInfoExtra = "CreatureDisplayInfoExtra.dbc";
    public const string CreatureFamily = "CreatureFamily.dbc";
    public const string CreatureModelData = "CreatureModelData.dbc";
    public const string CreatureSoundData = "CreatureSoundData.dbc";
    public const string CreatureSpellData = "CreatureSpellData.dbc";
    public const string CreatureType = "CreatureType.dbc";

    public static IReadOnlyList<string> CoreCreatureDbcFiles { get; } =
    [
        CreatureDisplayInfo,
        CreatureDisplayInfoExtra,
        CreatureFamily,
        CreatureModelData,
        CreatureSoundData,
        CreatureSpellData,
        CreatureType,
    ];
}
