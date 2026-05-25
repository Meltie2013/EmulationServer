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

using EmulationServer.Tools.Extraction.Formats.Maps.Conversion;
using EmulationServer.Tools.Extraction.Formats.Vmaps.Conversion;
using EmulationServer.Tools.Extraction.Mpq;

/**
  * File overview: tools/EmulationServer.Tools.Extraction/Extraction/GameDataExtractor.cs
  * Documents the GameDataExtractor source file in the client data extraction and conversion tooling area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Tools.Extraction.Extraction;

/**
  * Owns the game data extractor behavior for the client data extraction and conversion tooling layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class GameDataExtractor
{
    /**
      * Extracts data from source files and writes the normalized server format.
      * The method is part of GameDataExtractor and keeps this workflow isolated from the caller.
      */
    public IReadOnlyList<AssetExtractionResult> ExtractAll(AssetExtractionOptions options)
    {
        return
        [
            ExtractDbc(options),
            ExtractMaps(options),
            ExtractVmaps(options),
            ExtractMmaps(options),
        ];
    }

    /**
      * Extracts data from source files and writes the normalized server format.
      * The method is part of GameDataExtractor and keeps this workflow isolated from the caller.
      */
    public AssetExtractionResult ExtractDbc(AssetExtractionOptions options)
    {
        options.Validate();
        WowMpqArchiveSet archives = WowMpqArchiveSet.Discover(options.ClientRootDirectory, options.Locale);
        string outputDirectory = Path.Combine(options.OutputDirectory, "dbc");

        AssetCopyReport report = archives.ExtractKnownFileNames(
            KnownDbcFiles.All,
            static normalizedName => Path.GetFileName(normalizedName),
            outputDirectory,
            options.Overwrite,
            options.ReportProgress);

        return ToResult(AssetExtractionKind.Dbc, report, outputDirectory, archives);
    }

    /**
      * Extracts data from source files and writes the normalized server format.
      * The method is part of GameDataExtractor and keeps this workflow isolated from the caller.
      */
    public AssetExtractionResult ExtractMaps(AssetExtractionOptions options)
    {
        options.Validate();
        WowMpqArchiveSet archives = WowMpqArchiveSet.Discover(options.ClientRootDirectory, options.Locale);
        string rawOutputDirectory = Path.Combine(options.OutputDirectory, "maps-raw");
        string dbcOutputDirectory = Path.Combine(options.OutputDirectory, "dbc");
        string mapOutputDirectory = Path.Combine(options.OutputDirectory, "maps");

        EnsureMapConversionDbcFiles(archives, dbcOutputDirectory, options.Overwrite, options.ReportProgress);

        AssetCopyReport report = archives.ExtractKnownFiles(
            IsMapSourceFile,
            static normalizedName => normalizedName,
            rawOutputDirectory,
            options.Overwrite,
            options.ReportProgress);

        AssetExtractionResult result = ToResult(AssetExtractionKind.Maps, report, rawOutputDirectory, archives);
        MapConversionService conversionService = new();
        MapConversionResult conversion = conversionService.ConvertRawAdtDirectory(
            rawOutputDirectory,
            dbcOutputDirectory,
            mapOutputDirectory,
            options.Build,
            options.Overwrite,
            options.ReportProgress);

        for (int i = 0; i < conversion.ConvertedFiles; i++)
        {
            result.AddExtractedFile();
        }

        for (int i = 0; i < conversion.SkippedFiles; i++)
        {
            result.AddSkippedFile();
        }

        AddMessage(result, options.ReportProgress, $"Converted {conversion.ConvertedFiles} ADT source file(s) into server .map files.");
        AddMessage(result, options.ReportProgress, $"Generated .map output directory: {mapOutputDirectory}");

        if (conversion.FailedFiles > 0)
        {
            AddMessage(result, options.ReportProgress, $"Failed to convert {conversion.FailedFiles} ADT source file(s).");
        }

        foreach (string message in conversion.Messages)
        {
            result.AddMessage(message);
        }

        return result;
    }

    /**
      * Extracts data from source files and writes the normalized server format.
      * The method is part of GameDataExtractor and keeps this workflow isolated from the caller.
      */
    public AssetExtractionResult ExtractVmaps(AssetExtractionOptions options)
    {
        options.Validate();
        WowMpqArchiveSet archives = WowMpqArchiveSet.Discover(options.ClientRootDirectory, options.Locale);
        string rawOutputDirectory = Path.Combine(options.OutputDirectory, "vmaps-raw");
        string dbcOutputDirectory = Path.Combine(options.OutputDirectory, "dbc");
        string vmapOutputDirectory = Path.Combine(options.OutputDirectory, "vmaps");

        EnsureVmapConversionDbcFiles(archives, dbcOutputDirectory, options.Overwrite, options.ReportProgress);

        AssetCopyReport report = archives.ExtractKnownFiles(
            IsVmapSourceFile,
            static normalizedName => normalizedName,
            rawOutputDirectory,
            options.Overwrite,
            options.ReportProgress);

        AssetExtractionResult result = ToResult(AssetExtractionKind.Vmaps, report, rawOutputDirectory, archives);
        VmapConversionService conversionService = new();
        VmapConversionResult conversion = conversionService.ConvertRawVmapDirectory(
            rawOutputDirectory,
            dbcOutputDirectory,
            vmapOutputDirectory,
            options.Build,
            options.Overwrite,
            options.ReportProgress);

        for (int i = 0; i < conversion.ConvertedModelFiles + conversion.ConvertedPlacementFiles; i++)
        {
            result.AddExtractedFile();
        }

        for (int i = 0; i < conversion.SkippedModelFiles + conversion.SkippedPlacementFiles; i++)
        {
            result.AddSkippedFile();
        }

        AddMessage(result, options.ReportProgress, $"Converted {conversion.ConvertedModelFiles} WMO model file(s) and {conversion.ConvertedPlacementFiles} placement tile file(s) into Emulation Server compact vmap files.");
        AddMessage(result, options.ReportProgress, $"Generated vmap output directory: {vmapOutputDirectory}");

        if (conversion.FailedModelFiles > 0 || conversion.FailedPlacementFiles > 0)
        {
            AddMessage(result, options.ReportProgress, $"Failed to convert {conversion.FailedModelFiles} WMO model file(s) and {conversion.FailedPlacementFiles} placement tile file(s).");
        }

        foreach (string message in conversion.Messages)
        {
            result.AddMessage(message);
        }

        return result;
    }

    /**
      * Extracts data from source files and writes the normalized server format.
      * The method is part of GameDataExtractor and keeps this workflow isolated from the caller.
      */
    public AssetExtractionResult ExtractMmaps(AssetExtractionOptions options)
    {
        options.Validate();
        string outputDirectory = Path.Combine(options.OutputDirectory, "mmaps");
        Directory.CreateDirectory(outputDirectory);

        AssetExtractionResult result = new(AssetExtractionKind.Mmaps);
        AddMessage(result, options.ReportProgress, "MMaps are generated navmesh data derived from extracted maps/vmaps. Native Recast/Detour mmap generation is not implemented yet in the C# tool.");
        AddMessage(result, options.ReportProgress, $"Created mmap output directory: {outputDirectory}");
        string readmePath = Path.Combine(outputDirectory, "README.txt");
        File.WriteAllText(
            readmePath,
            "MMap generation is not implemented yet in MapDataTool. This directory is reserved for generated navigation mesh files.\n");
        AddMessage(result, options.ReportProgress, "Created 'README.txt'");
        return result;
    }

    /**
      * Validates ensure map conversion dbc files state before it is used by another server component.
      * Validation failures are raised as close to the source as possible so configuration, packet, and data problems are easier to diagnose.
      * Inputs used by this operation: archives, dbcOutputDirectory, overwrite, progressMessage.
      */
    private static void EnsureMapConversionDbcFiles(WowMpqArchiveSet archives, string dbcOutputDirectory, bool overwrite, Action<string>? progressMessage)
    {
        string mapDbcPath = Path.Combine(dbcOutputDirectory, "Map.dbc");
        string areaTableDbcPath = Path.Combine(dbcOutputDirectory, "AreaTable.dbc");
        string liquidTypeDbcPath = Path.Combine(dbcOutputDirectory, "LiquidType.dbc");

        if (File.Exists(mapDbcPath) && File.Exists(areaTableDbcPath) && File.Exists(liquidTypeDbcPath))
        {
            return;
        }

        archives.ExtractKnownFileNames(
            [
                "DBFilesClient/Map.dbc",
                "DBFilesClient/AreaTable.dbc",
                "DBFilesClient/LiquidType.dbc",
            ],
            static normalizedName => Path.GetFileName(normalizedName),
            dbcOutputDirectory,
            overwrite: true,
            progressMessage: progressMessage);
    }

    /**
      * Ensures the DBC files needed by vmap placement conversion are available.
      * Map.dbc is required to translate ADT directory names into map identifiers.
      */
    private static void EnsureVmapConversionDbcFiles(WowMpqArchiveSet archives, string dbcOutputDirectory, bool overwrite, Action<string>? progressMessage)
    {
        string mapDbcPath = Path.Combine(dbcOutputDirectory, "Map.dbc");

        if (File.Exists(mapDbcPath))
        {
            return;
        }

        archives.ExtractKnownFileNames(
            [
                "DBFilesClient/Map.dbc",
            ],
            static normalizedName => Path.GetFileName(normalizedName),
            dbcOutputDirectory,
            overwrite: true,
            progressMessage: progressMessage);
    }

    /**
      * Adds a new item to the managed collection while preserving internal invariants.
      * The method is part of GameDataExtractor and keeps this workflow isolated from the caller.
      */
    private static void AddMessage(AssetExtractionResult result, Action<string>? progressMessage, string message)
    {
        result.AddMessage(message);
        progressMessage?.Invoke(message);
    }

    /**
      * Performs the to result operation for the client data extraction and conversion tooling workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: kind, report, outputDirectory, archives.
      */
    private static AssetExtractionResult ToResult(
        AssetExtractionKind kind,
        AssetCopyReport report,
        string outputDirectory,
        WowMpqArchiveSet archives)
    {
        AssetExtractionResult result = new(kind);

        for (int i = 0; i < report.ExtractedFiles; i++)
        {
            result.AddExtractedFile();
        }

        for (int i = 0; i < report.SkippedExisting; i++)
        {
            result.AddSkippedFile();
        }

        result.AddMessage($"Scanned {archives.Archives.Count} MPQ archive(s).");
        result.AddMessage($"Found {report.CandidateFiles} candidate file(s).");
        result.AddMessage($"Extracted {report.ExtractedFiles} file(s) to {outputDirectory}.");

        if (report.SkippedExisting > 0)
        {
            result.AddMessage($"Skipped {report.SkippedExisting} existing file(s).");
        }

        if (report.FailedFiles > 0)
        {
            result.AddMessage($"Failed to extract {report.FailedFiles} file(s). See messages below for details.");
        }

        if (report.UnknownFileNames > 0)
        {
            result.AddMessage($"Ignored {report.UnknownFileNames} archive entrie(s) without known filenames. MPQs without listfiles may require a generated listfile before full extraction is possible.");
        }

        foreach (string message in report.Messages)
        {
            result.AddMessage(message);
        }

        return result;
    }

    /**
      * Determines whether dbc file for the client data extraction and conversion tooling workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: normalizedName.
      */
    private static bool IsDbcFile(string normalizedName)
    {
        return normalizedName.StartsWith("DBFilesClient/", StringComparison.OrdinalIgnoreCase) &&
               normalizedName.EndsWith(".dbc", StringComparison.OrdinalIgnoreCase);
    }

    /**
      * Determines whether map source file for the client data extraction and conversion tooling workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: normalizedName.
      */
    private static bool IsMapSourceFile(string normalizedName)
    {
        if (!normalizedName.StartsWith("World/Maps/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return normalizedName.EndsWith(".wdt", StringComparison.OrdinalIgnoreCase) ||
               normalizedName.EndsWith(".wdl", StringComparison.OrdinalIgnoreCase) ||
               normalizedName.EndsWith(".adt", StringComparison.OrdinalIgnoreCase);
    }

    /**
      * Determines whether vmap source file for the client data extraction and conversion tooling workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: normalizedName.
      */
    private static bool IsVmapSourceFile(string normalizedName)
    {
        if (normalizedName.EndsWith(".wmo", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return normalizedName.StartsWith("World/Maps/", StringComparison.OrdinalIgnoreCase) &&
               normalizedName.EndsWith(".adt", StringComparison.OrdinalIgnoreCase);
    }
}
