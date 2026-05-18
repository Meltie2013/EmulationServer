//
// Copyright (C) 2026 Emulation Server Project
//
// This program is free software. You can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation. either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY. Without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//

using EmulationServer.Tools.Extraction.Formats.Adt;

/**
  * File overview: tools/EmulationServer.Tools.Extraction/Formats/Maps/Conversion/MapConversionService.cs
  * This file belongs to the developer tooling for data extraction, validation, and diagnostics portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Tools.Extraction.Formats.Maps.Conversion;

/**
  * Represents the map conversion service component in the developer tooling for data extraction, validation, and diagnostics area.
  * It encapsulates a focused runtime behavior so callers can use a small public API instead of duplicating workflow code.
  */
public sealed class MapConversionService
{
    /**
      * Performs the convert raw adt directory operation for MapConversionService.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
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
    /**
      * Adds a new item to the managed collection while preserving internal invariants.
      * The method is part of MapConversionService and keeps this workflow isolated from the caller.
      */
    private static void AddMessage(MapConversionResult result, Action<string>? progressMessage, string message)
    {
        result.Messages.Add(message);
        progressMessage?.Invoke(message);
    }
}
