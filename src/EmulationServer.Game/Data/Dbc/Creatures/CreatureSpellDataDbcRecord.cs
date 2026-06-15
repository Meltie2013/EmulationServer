namespace EmulationServer.Game.Data.Dbc.Creatures;

public sealed record CreatureSpellDataDbcRecord(
    int Id,
    IReadOnlyList<int> SpellIds,
    IReadOnlyList<int> Cooldowns);
