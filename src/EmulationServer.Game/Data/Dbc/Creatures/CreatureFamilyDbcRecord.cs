namespace EmulationServer.Game.Data.Dbc.Creatures;

public sealed record CreatureFamilyDbcRecord(
    int Id,
    float MinScale,
    int MinScaleLevel,
    float MaxScale,
    int MaxScaleLevel,
    int PetFoodMask,
    int SkillLine2,
    int PetTalentType,
    int CategoryEnumId,
    string Name,
    string IconFile);
