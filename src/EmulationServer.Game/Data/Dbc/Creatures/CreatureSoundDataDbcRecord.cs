namespace EmulationServer.Game.Data.Dbc.Creatures;

public sealed record CreatureSoundDataDbcRecord(
    int Id,
    IReadOnlyList<int> SoundIds,
    int CreatureImpactType,
    int FidgetDelaySecondsMin,
    int FidgetDelaySecondsMax);
