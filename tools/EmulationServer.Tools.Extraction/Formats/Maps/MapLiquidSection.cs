
namespace EmulationServer.Tools.Extraction.Formats.Maps;

public sealed record MapLiquidSection(
    ushort Flags,
    ushort LiquidType,
    byte OffsetX,
    byte OffsetY,
    byte Width,
    byte Height,
    float LiquidLevel)
{
    public bool HasLiquidType => (Flags & MapFormatConstants.MapLiquidNoType) == 0;

    public bool HasLiquidHeight => (Flags & MapFormatConstants.MapLiquidNoHeight) == 0;
}
