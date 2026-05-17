
using EmulationServer.Tools.Extraction.Formats.Adt;

namespace EmulationServer.Tools.Extraction.Formats.Maps.Conversion;

public sealed class MapConversionService
{
    public MapConversionResult ConvertRawAdtDirectory(
        string rawMapDirectory,
        string dbcDirectory,
        string outputDirectory,
        ushort build,
        bool overwrite,
        Action<string>? progressMessage = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawMapDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(dbcDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        MapConversionResult result = new();

        if (!Directory.Exists(rawMapDirectory))
        {
            AddMessage(result, progressMessage, $"Raw map directory does not exist: {rawMapDirectory}");
            return result;
        }

        string mapDbcPath = Path.Combine(dbcDirectory, "Map.dbc");
        if (!File.Exists(mapDbcPath))
        {
            AddMessage(result, progressMessage, $"Map.dbc was not found at {mapDbcPath}. Run extract-dbc before extract-maps, or use extract-all.");
            return result;
        }

        string areaTableDbcPath = Path.Combine(dbcDirectory, "AreaTable.dbc");
        string liquidTypeDbcPath = Path.Combine(dbcDirectory, "LiquidType.dbc");

        MapDbcIndex mapIndex = MapDbcIndex.Load(mapDbcPath);

        AreaTableIndex areaTable = File.Exists(areaTableDbcPath)
            ? AreaTableIndex.Load(areaTableDbcPath)
            : AreaTableIndex.Empty;

        LiquidTypeIndex liquidTypes = File.Exists(liquidTypeDbcPath)
            ? LiquidTypeIndex.Load(liquidTypeDbcPath)
            : LiquidTypeIndex.Empty;

        if (!File.Exists(liquidTypeDbcPath))
        {
            AddMessage(result, progressMessage, $"LiquidType.dbc was not found at {liquidTypeDbcPath}. Liquid type flags will be limited.");
        }

        MangosMapTileConverter converter = new(areaTable, liquidTypes);

        Directory.CreateDirectory(outputDirectory);

        foreach (string adtPath in Directory.EnumerateFiles(rawMapDirectory, "*.adt", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            result.SourceFiles++;

            if (!AdtFileNameParser.TryParse(adtPath, out string mapDirectoryName, out int tileX, out int tileY))
            {
                result.SkippedFiles++;
                continue;
            }

            if (!mapIndex.TryGetByDirectoryName(mapDirectoryName, out MapDbcEntry mapEntry))
            {
                result.SkippedFiles++;
                AddMessage(result, progressMessage, $"Skipped '{Path.GetFileName(adtPath)}' because Map.dbc does not contain map directory '{mapDirectoryName}'.");
                continue;
            }

            string outputPath = Path.Combine(outputDirectory, $"{mapEntry.Id:D3}{tileX:D2}{tileY:D2}.map");

            if (!overwrite && File.Exists(outputPath))
            {
                result.SkippedFiles++;
                AddMessage(result, progressMessage, $"Skipped existing '{Path.GetFileName(outputPath)}'");
                continue;
            }

            try
            {
                MapTileConversionReport tileReport = converter.Convert(adtPath, outputPath, build);

                result.ConvertedFiles++;

                AddMessage(
                    result,
                    progressMessage,
                    $"Converted '{Path.GetFileName(outputPath)}' from '{Path.GetFileName(adtPath)}' | Liquid Data: {tileReport.GetLiquidStatus()}");
            }
            catch (AdtFormatException exception) when (exception.Message.Contains("does not contain any MCNK chunks", StringComparison.OrdinalIgnoreCase))
            {
                result.SkippedFiles++;
                AddMessage(result, progressMessage, $"Skipped '{Path.GetFileName(adtPath)}' because it does not contain terrain chunks.");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException or NotSupportedException or AdtFormatException)
            {
                result.FailedFiles++;
                AddMessage(result, progressMessage, $"Failed to convert '{Path.GetFileName(adtPath)}': {exception.Message}");
            }
        }

        AddMessage(result, progressMessage, $"Converted {result.ConvertedFiles} MaNGOS .map file(s) to {outputDirectory}.");

        if (result.FailedFiles > 0)
        {
            AddMessage(result, progressMessage, $"Failed to convert {result.FailedFiles} ADT source file(s). See messages above for details.");
        }

        return result;
    }
    private static void AddMessage(MapConversionResult result, Action<string>? progressMessage, string message)
    {
        result.Messages.Add(message);
        progressMessage?.Invoke(message);
    }
}
