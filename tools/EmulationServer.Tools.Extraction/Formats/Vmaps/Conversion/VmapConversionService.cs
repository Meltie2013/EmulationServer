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

using System.Text;
using System.Text.RegularExpressions;
using EmulationServer.Tools.Extraction.Formats.Maps.Conversion;

/**
  * File overview: tools/EmulationServer.Tools.Extraction/Formats/Vmaps/Conversion/VmapConversionService.cs
  * This file coordinates WMO model conversion and ADT placement tile generation for vmaps.
  */

namespace EmulationServer.Tools.Extraction.Formats.Vmaps.Conversion;

/**
  * Converts extracted client WMO and ADT placement sources into compact Emulation Server vmap files.
  * The first implementation focuses on WMO geometry and WMO placements; M2/SKIN collision conversion is intentionally left for a later pass.
  */
public sealed partial class VmapConversionService
{
    private const string ModelsDirectoryName = "models";
    private const string TilesDirectoryName = "tiles";
    private const string ManifestFileName = "vmap_manifest.txt";

    /**
      * Converts raw vmap source files into compact model and placement output.
      */
    public VmapConversionResult ConvertRawVmapDirectory(
        string rawVmapDirectory,
        string dbcDirectory,
        string outputDirectory,
        ushort build,
        bool overwrite,
        Action<string>? progressMessage = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawVmapDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(dbcDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        VmapConversionResult result = new();

        if (!Directory.Exists(rawVmapDirectory))
        {
            AddMessage(result, progressMessage, $"Raw vmap directory does not exist: {rawVmapDirectory}");
            return result;
        }

        Directory.CreateDirectory(outputDirectory);

        string modelOutputDirectory = Path.Combine(outputDirectory, ModelsDirectoryName);
        string tileOutputDirectory = Path.Combine(outputDirectory, TilesDirectoryName);
        Directory.CreateDirectory(modelOutputDirectory);
        Directory.CreateDirectory(tileOutputDirectory);

        Dictionary<string, VmapModel> convertedModels = ConvertModels(rawVmapDirectory, modelOutputDirectory, build, overwrite, result, progressMessage);
        ConvertPlacementTiles(rawVmapDirectory, dbcDirectory, tileOutputDirectory, build, overwrite, result, progressMessage);
        WriteManifest(outputDirectory, rawVmapDirectory, convertedModels.Values.OrderBy(model => model.Name.NormalizedPath, StringComparer.OrdinalIgnoreCase), result, progressMessage);

        AddMessage(result, progressMessage, $"Converted {result.ConvertedModelFiles} WMO model file(s) and {result.ConvertedPlacementFiles} vmap placement tile file(s) to {outputDirectory}.");
        return result;
    }

    /**
      * Converts root WMO files and their group files into compact model files.
      */
    private static Dictionary<string, VmapModel> ConvertModels(
        string rawVmapDirectory,
        string outputDirectory,
        ushort build,
        bool overwrite,
        VmapConversionResult result,
        Action<string>? progressMessage)
    {
        Dictionary<string, VmapModel> convertedModels = new(StringComparer.OrdinalIgnoreCase);

        foreach (string rootPath in EnumerateRootWmoFiles(rawVmapDirectory))
        {
            result.SourceModelFiles++;
            string relativePath = Path.GetRelativePath(rawVmapDirectory, rootPath).Replace(Path.DirectorySeparatorChar, '/');
            VmapModelName modelName = VmapModelName.FromRelativePath(relativePath);
            string outputPath = Path.Combine(outputDirectory, $"{modelName.Key}.vmapmodel");

            if (!overwrite && File.Exists(outputPath))
            {
                result.SkippedModelFiles++;
                AddMessage(result, progressMessage, $"Skipped existing vmap model '{Path.GetFileName(outputPath)}'");
                continue;
            }

            try
            {
                VmapModel model = BuildModel(rootPath, modelName);

                if (model.Groups.Count == 0 || model.TriangleCount == 0)
                {
                    result.SkippedModelFiles++;
                    AddMessage(result, progressMessage, $"Skipped WMO '{relativePath}' because it does not contain convertible group geometry.");
                    continue;
                }

                VmapModelWriter.Write(outputPath, model, build);
                convertedModels[model.Name.NormalizedPath] = model;
                result.ConvertedModelFiles++;
                AddMessage(result, progressMessage, $"Converted vmap model '{Path.GetFileName(outputPath)}' from '{relativePath}' | groups={model.Groups.Count}, vertices={model.VertexCount}, triangles={model.TriangleCount}");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException or NotSupportedException)
            {
                result.FailedModelFiles++;
                AddMessage(result, progressMessage, $"Failed to convert WMO '{relativePath}': {exception.Message}");
            }
        }

        return convertedModels;
    }

    /**
      * Builds a compact model from a root WMO and its numbered group WMO files.
      */
    private static VmapModel BuildModel(string rootPath, VmapModelName modelName)
    {
        WmoRootInfo rootInfo = WmoRootReader.Read(rootPath);
        List<WmoGroupGeometry> groups = [];

        for (int groupIndex = 0; groupIndex < rootInfo.GroupCount; groupIndex++)
        {
            string groupPath = GetGroupPath(rootPath, groupIndex);

            if (!File.Exists(groupPath))
            {
                continue;
            }

            WmoGroupGeometry group = WmoGroupReader.Read(groupPath, groupIndex);

            if (group.Vertices.Count > 0 && group.Indices.Count >= 3)
            {
                groups.Add(group);
            }
        }

        return new VmapModel(modelName, groups);
    }

    /**
      * Converts ADT WMO placement data into one placement file per map tile.
      */
    private static void ConvertPlacementTiles(
        string rawVmapDirectory,
        string dbcDirectory,
        string outputDirectory,
        ushort build,
        bool overwrite,
        VmapConversionResult result,
        Action<string>? progressMessage)
    {
        string mapDbcPath = Path.Combine(dbcDirectory, "Map.dbc");

        if (!File.Exists(mapDbcPath))
        {
            AddMessage(result, progressMessage, $"Map.dbc was not found at {mapDbcPath}. WMO model files were converted, but vmap placement tiles could not be generated.");
            return;
        }

        MapDbcIndex mapIndex = MapDbcIndex.Load(mapDbcPath);

        foreach (string adtPath in Directory.EnumerateFiles(rawVmapDirectory, "*.adt", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            result.SourcePlacementFiles++;

            if (!AdtFileNameParser.TryParse(adtPath, out string mapDirectoryName, out int tileX, out int tileY))
            {
                result.SkippedPlacementFiles++;
                continue;
            }

            if (!mapIndex.TryGetByDirectoryName(mapDirectoryName, out MapDbcEntry mapEntry))
            {
                result.SkippedPlacementFiles++;
                AddMessage(result, progressMessage, $"Skipped vmap placement file '{Path.GetFileName(adtPath)}' because Map.dbc does not contain map directory '{mapDirectoryName}'.");
                continue;
            }

            string outputPath = Path.Combine(outputDirectory, $"{mapEntry.Id:D3}{tileX:D2}{tileY:D2}.vmaptile");

            if (!overwrite && File.Exists(outputPath))
            {
                result.SkippedPlacementFiles++;
                AddMessage(result, progressMessage, $"Skipped existing vmap tile '{Path.GetFileName(outputPath)}'");
                continue;
            }

            try
            {
                IReadOnlyList<VmapPlacement> placements = AdtWmoPlacementReader.Read(adtPath);

                if (placements.Count == 0)
                {
                    result.SkippedPlacementFiles++;
                    continue;
                }

                VmapPlacementTile tile = new(mapEntry.Id, tileX, tileY, placements);
                VmapPlacementTileWriter.Write(outputPath, tile, build);
                result.ConvertedPlacementFiles++;
                AddMessage(result, progressMessage, $"Converted vmap tile '{Path.GetFileName(outputPath)}' from '{Path.GetFileName(adtPath)}' | placements={placements.Count}");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException or NotSupportedException)
            {
                result.FailedPlacementFiles++;
                AddMessage(result, progressMessage, $"Failed to convert vmap placement file '{Path.GetFileName(adtPath)}': {exception.Message}");
            }
        }
    }

    /**
      * Writes a human-readable manifest that links generated compact model files back to source WMO paths.
      */
    private static void WriteManifest(
        string outputDirectory,
        string rawVmapDirectory,
        IEnumerable<VmapModel> models,
        VmapConversionResult result,
        Action<string>? progressMessage)
    {
        string manifestPath = Path.Combine(outputDirectory, ManifestFileName);
        StringBuilder builder = new();
        builder.AppendLine("Emulation Server VMap Manifest");
        builder.AppendLine("===============================");
        builder.AppendLine();
        builder.AppendLine($"RawSourceDirectory={rawVmapDirectory}");
        builder.AppendLine($"GeneratedUtc={DateTimeOffset.UtcNow:O}");
        builder.AppendLine();
        builder.AppendLine("Models:");

        foreach (VmapModel model in models)
        {
            builder.AppendLine($"  {model.Name.Key}.vmapmodel | Source={model.Name.NormalizedPath} | Groups={model.Groups.Count} | Vertices={model.VertexCount} | Triangles={model.TriangleCount}");
        }

        File.WriteAllText(manifestPath, builder.ToString());
        AddMessage(result, progressMessage, $"Created '{ManifestFileName}'");
    }

    /**
      * Enumerates root WMO files while excluding numbered group files such as SomeBuilding_000.wmo.
      */
    private static IEnumerable<string> EnumerateRootWmoFiles(string rawVmapDirectory)
    {
        return Directory.EnumerateFiles(rawVmapDirectory, "*.wmo", SearchOption.AllDirectories)
            .Where(static path => !GroupWmoFileRegex().IsMatch(Path.GetFileNameWithoutExtension(path)))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);
    }

    /**
      * Builds the expected numbered group WMO path for a root WMO path.
      */
    private static string GetGroupPath(string rootPath, int groupIndex)
    {
        string directory = Path.GetDirectoryName(rootPath) ?? string.Empty;
        string rootName = Path.GetFileNameWithoutExtension(rootPath);
        return Path.Combine(directory, $"{rootName}_{groupIndex:D3}.wmo");
    }

    /**
      * Adds a conversion message and optionally streams it to the console progress callback.
      */
    private static void AddMessage(VmapConversionResult result, Action<string>? progressMessage, string message)
    {
        result.Messages.Add(message);
        progressMessage?.Invoke(message);
    }

    [GeneratedRegex("_\\d{3}$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    /**
      * Returns the group WMO naming pattern used to identify files such as Root_000.wmo.
      */
    private static partial Regex GroupWmoFileRegex();
}
