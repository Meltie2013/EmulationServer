
namespace EmulationServer.Tools.Extraction.Formats.Maps;

public sealed record MapFileHeader(
    string MapMagic,
    string VersionMagic,
    uint Build,
    uint AreaMapOffset,
    uint AreaMapSize,
    uint HeightMapOffset,
    uint HeightMapSize,
    uint LiquidMapOffset,
    uint LiquidMapSize,
    uint HolesOffset,
    uint HolesSize);
