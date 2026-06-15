namespace EmulationServer.Game.Data.Dbc.Creatures;

public sealed record CreatureModelDataDbcRecord(
    int Id,
    int Flags,
    string ModelName,
    int SizeClass,
    float ModelScale,
    int BloodId,
    int FootprintTextureId,
    float FootprintTextureLength,
    float FootprintTextureWidth,
    float FootprintTextureScale,
    int FoleyMaterialId,
    int FootstepShakeSize,
    int DeathThudShakeSize,
    float CollisionWidth,
    float CollisionHeight,
    float MountHeight);
