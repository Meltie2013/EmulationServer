namespace EmulationServer.WorldServer.Configuration;

public sealed class GameDataSettings
{
    public bool Enabled { get; init; }

    public string DataDirectory { get; init; } = "Data";

    public string DbcDirectory { get; init; } = "dbc";

    public IReadOnlyList<string> RequiredDbcFiles { get; init; } = DefaultRequiredDbcFiles;

    public static IReadOnlyList<string> DefaultRequiredDbcFiles { get; } =
    [
        "AreaTable.dbc",
        "AreaTrigger.dbc",
        "AuctionHouse.dbc",
        "BankBagSlotPrices.dbc",
        "CharStartOutfit.dbc",
        "ChatChannels.dbc",
        "ChrClasses.dbc",
        "ChrRaces.dbc",
        "CinematicSequences.dbc",
        "DurabilityCosts.dbc",
        "DurabilityQuality.dbc",
        "Emotes.dbc",
        "EmotesText.dbc",
        "Faction.dbc",
        "FactionTemplate.dbc",
        "ItemBagFamily.dbc",
        "ItemClass.dbc",
        "ItemRandomProperties.dbc",
        "ItemSet.dbc",
        "Lock.dbc",
        "MailTemplate.dbc",
        "Map.dbc",
        "QuestSort.dbc",
        "SkillLine.dbc",
        "SkillLineAbility.dbc",
        "SkillRaceClassInfo.dbc",
        "SoundEntries.dbc",
        "Spell.dbc",
        "SpellCastTimes.dbc",
        "SpellDuration.dbc",
        "SpellFocusObject.dbc",
        "SpellItemEnchantment.dbc",
        "SpellRadius.dbc",
        "SpellRange.dbc",
        "SpellShapeshiftForm.dbc",
        "StableSlotPrices.dbc",
        "Talent.dbc",
        "TalentTab.dbc",
        "TaxiNodes.dbc",
        "TaxiPath.dbc",
        "TaxiPathNode.dbc",
        "WMOAreaTable.dbc",
        "WorldMapArea.dbc",
        "WorldSafeLocs.dbc",
    ];

    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(DataDirectory))
        {
            throw new InvalidOperationException("WorldServer game data directory is required when game data loading is enabled.");
        }

        if (string.IsNullOrWhiteSpace(DbcDirectory))
        {
            throw new InvalidOperationException("WorldServer DBC directory is required when game data loading is enabled.");
        }

        if (RequiredDbcFiles.Count == 0)
        {
            throw new InvalidOperationException("At least one required DBC file must be configured when game data loading is enabled.");
        }

        foreach (string requiredDbcFile in RequiredDbcFiles)
        {
            if (string.IsNullOrWhiteSpace(requiredDbcFile))
            {
                throw new InvalidOperationException("Required DBC file list cannot contain empty entries.");
            }
        }
    }
}
