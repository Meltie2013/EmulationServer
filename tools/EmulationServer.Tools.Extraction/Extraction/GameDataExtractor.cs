
using EmulationServer.Tools.Extraction.Formats.Maps.Conversion;
using EmulationServer.Tools.Extraction.Mpq;

namespace EmulationServer.Tools.Extraction.Extraction;

public sealed class GameDataExtractor
{
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

        AddMessage(result, options.ReportProgress, $"Converted {conversion.ConvertedFiles} ADT source file(s) into MaNGOS .map files.");
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

    public AssetExtractionResult ExtractVmaps(AssetExtractionOptions options)
    {
        options.Validate();
        WowMpqArchiveSet archives = WowMpqArchiveSet.Discover(options.ClientRootDirectory, options.Locale);
        string outputDirectory = Path.Combine(options.OutputDirectory, "vmaps-raw");

        AssetCopyReport report = archives.ExtractKnownFiles(
            IsVmapSourceFile,
            static normalizedName => normalizedName,
            outputDirectory,
            options.Overwrite,
            options.ReportProgress);

        AssetExtractionResult result = ToResult(AssetExtractionKind.Vmaps, report, outputDirectory, archives);
        AddMessage(result, options.ReportProgress, "VMap source extraction completed. This extracts raw WMO/M2/SKIN source files. MaNGOS vmap assembly is not implemented yet in the C# tool.");
        return result;
    }

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

    private static void AddMessage(AssetExtractionResult result, Action<string>? progressMessage, string message)
    {
        result.AddMessage(message);
        progressMessage?.Invoke(message);
    }

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

    private static bool IsDbcFile(string normalizedName)
    {
        return normalizedName.StartsWith("DBFilesClient/", StringComparison.OrdinalIgnoreCase) &&
               normalizedName.EndsWith(".dbc", StringComparison.OrdinalIgnoreCase);
    }

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

    private static bool IsVmapSourceFile(string normalizedName)
    {
        return normalizedName.EndsWith(".wmo", StringComparison.OrdinalIgnoreCase) ||
               normalizedName.EndsWith(".m2", StringComparison.OrdinalIgnoreCase) ||
               normalizedName.EndsWith(".skin", StringComparison.OrdinalIgnoreCase);
    }
}
