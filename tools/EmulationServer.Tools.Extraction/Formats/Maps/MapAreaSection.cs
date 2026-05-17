
namespace EmulationServer.Tools.Extraction.Formats.Maps;

public sealed record MapAreaSection(
    ushort Flags,
    ushort GridArea,
    IReadOnlyList<ushort> AreaFlags)
{
    public bool HasFullAreaData => (Flags & MapFormatConstants.MapAreaNoArea) == 0;
}
