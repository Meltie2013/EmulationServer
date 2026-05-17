
using EmulationServer.Tools.Extraction.Client;
using EmulationServer.Tools.Extraction.Formats.Maps;

namespace EmulationServer.Tools.Extraction.Validation;

public sealed class MapDataVerifier
{
    public MapValidationResult VerifyFile(string path)
    {
        MapValidationResult result = new();

        try
        {
            ExtractedMapFile map = ExtractedMapFileReader.Read(path);
            Verify(map, result);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or MapFormatException or EndOfStreamException)
        {
            result.AddError($"{path}: {exception.Message}");
        }

        return result;
    }

    public MapValidationResult Verify(ExtractedMapFile map)
    {
        MapValidationResult result = new();
        Verify(map, result);
        return result;
    }

    private static void Verify(ExtractedMapFile map, MapValidationResult result)
    {
        result.AddInfo($"{Path.GetFileName(map.Path)}: build={map.Header.Build}, area={(map.Area is null ? "no" : "yes")}, height={(map.Height is null ? "no" : "yes")}, liquid={(map.Liquid is null ? "no" : "yes")}");

        if (map.Header.Build > 0 && !ClientBuilds.IsSupported((ushort)map.Header.Build))
        {
            result.AddWarning($"{map.Path}: map build {map.Header.Build} is not one of the currently configured extraction target builds.");
        }

        if (map.Height is null)
        {
            result.AddWarning($"{map.Path}: map has no height section.");
        }
        else
        {
            VerifyHeightSection(map.Path, map.Height, result);
        }

        if (map.Liquid is not null)
        {
            VerifyLiquidSection(map.Path, map.Liquid, map.Header.LiquidMapSize, result);
        }

        if (map.HolesByteCount is not 0 and not 512)
        {
            result.AddWarning($"{map.Path}: holes section size is {map.HolesByteCount} byte(s). MaNGOS-style map tiles normally use 16x16 ushort hole data, or 512 bytes.");
        }
    }

    private static void VerifyHeightSection(string path, MapHeightSection height, MapValidationResult result)
    {
        if (!height.HasHeight)
        {
            result.AddInfo($"{path}: height section is marked as no-height.");
            return;
        }

        if (!float.IsFinite(height.GridHeight) || !float.IsFinite(height.GridMaxHeight))
        {
            result.AddError($"{path}: height section contains non-finite grid height values.");
            return;
        }

        if (height.GridMaxHeight < height.GridHeight)
        {
            result.AddError($"{path}: grid max height {height.GridMaxHeight} is lower than grid min height {height.GridHeight}.");
            return;
        }

        if (height.IsInt8Encoded)
        {
            float maxError = MapHeightCodec.MaximumUInt8Error(height.GridHeight, height.GridMaxHeight);
            result.AddInfo($"{path}: height data is uint8 encoded. Maximum round-trip quantization error is approximately {maxError:0.000000}.");
        }
        else if (height.IsInt16Encoded)
        {
            float maxError = MapHeightCodec.MaximumUInt16Error(height.GridHeight, height.GridMaxHeight);
            result.AddInfo($"{path}: height data is uint16 encoded. Maximum round-trip quantization error is approximately {maxError:0.000000}.");
        }
        else if (height.IsFloatEncoded)
        {
            result.AddInfo($"{path}: height data is stored as raw floats.");
        }

        if (height.V9ValueCount != MapFormatConstants.V9VertexCount || height.V8ValueCount != MapFormatConstants.V8VertexCount)
        {
            result.AddError($"{path}: height vertex counts are invalid. V9={height.V9ValueCount}, V8={height.V8ValueCount}.");
        }
    }

    private static void VerifyLiquidSection(string path, MapLiquidSection liquid, uint liquidMapSize, MapValidationResult result)
    {
        result.AddInfo(
            $"{path}: liquid type={liquid.LiquidType}, " +
            $"flags=0x{liquid.Flags:X4}, " +
            $"offset=({liquid.OffsetX},{liquid.OffsetY}), " +
            $"size={liquid.Width}x{liquid.Height}, " +
            $"level={liquid.LiquidLevel:0.###}, " +
            $"bytes={liquidMapSize}.");

        if (liquid.Width == 0 || liquid.Height == 0)
        {
            result.AddError($"{path}: liquid section has invalid dimensions {liquid.Width}x{liquid.Height}.");
        }

        if (liquid.OffsetX > MapFormatConstants.AdtGridSize || liquid.OffsetY > MapFormatConstants.AdtGridSize)
        {
            result.AddError($"{path}: liquid offset is outside the ADT grid.");
        }

        if (liquid.OffsetX + liquid.Width > MapFormatConstants.AdtGridSize + 1 ||
            liquid.OffsetY + liquid.Height > MapFormatConstants.AdtGridSize + 1)
        {
            result.AddError($"{path}: liquid bounds exceed the ADT grid.");
        }

        long expectedMinimum = 16;

        if (liquid.HasLiquidType)
        {
            expectedMinimum += MapFormatConstants.AreaCellCount * sizeof(ushort);
            expectedMinimum += MapFormatConstants.AreaCellCount;
        }

        if (liquid.HasLiquidHeight)
        {
            expectedMinimum += checked((long)liquid.Width * liquid.Height * sizeof(float));
        }

        if (liquidMapSize != expectedMinimum)
        {
            result.AddWarning(
                $"{path}: liquid section size is {liquidMapSize} byte(s), " +
                $"expected {expectedMinimum} byte(s) for this liquid header.");
        }
    }
}
