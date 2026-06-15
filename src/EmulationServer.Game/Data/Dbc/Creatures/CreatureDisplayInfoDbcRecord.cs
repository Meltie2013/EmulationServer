namespace EmulationServer.Game.Data.Dbc.Creatures;

public sealed record CreatureDisplayInfoDbcRecord(
    int Id,
    int ModelId,
    int SoundId,
    int ExtendedDisplayInfoId,
    float CreatureModelScale,
    int CreatureModelAlpha,
    string TextureVariation1,
    string TextureVariation2,
    string TextureVariation3,
    int SizeClass,
    int BloodId,
    int NPCSoundId);
