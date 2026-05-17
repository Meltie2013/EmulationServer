
namespace EmulationServer.Tools.Extraction.Formats.Maps;

public sealed record MapHeightSection(
    uint Flags,
    float GridHeight,
    float GridMaxHeight,
    int V9ValueCount,
    int V8ValueCount)
{
    public bool HasHeight => (Flags & MapFormatConstants.MapHeightNoHeight) == 0;

    public bool IsInt8Encoded => (Flags & MapFormatConstants.MapHeightAsInt8) != 0;

    public bool IsInt16Encoded => (Flags & MapFormatConstants.MapHeightAsInt16) != 0;

    public bool IsFloatEncoded => HasHeight && !IsInt8Encoded && !IsInt16Encoded;

    public float HeightRange => GridMaxHeight - GridHeight;
}
