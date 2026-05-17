
namespace EmulationServer.Tools.Extraction.Formats.Adt;

public sealed class AdtCell
{
    public int IndexX { get; init; }

    public int IndexY { get; init; }

    public uint Flags { get; init; }

    public uint AreaId { get; init; }

    public ushort Holes { get; init; }

    public float BaseHeight { get; init; }

    public float[] Heights { get; init; } = [];

    public bool HasHeights => Heights.Length == 145;
}
