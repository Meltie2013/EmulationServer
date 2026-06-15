namespace EmulationServer.Game.Data.Dbc.Creatures;

public sealed record CreatureDisplayInfoExtraDbcRecord(
    int Id,
    int DisplayRaceId,
    int DisplaySexId,
    int SkinId,
    int FaceId,
    int HairStyleId,
    int HairColorId,
    int FacialHairId,
    IReadOnlyList<int> NPCItemDisplayIds);
