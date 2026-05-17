
using EmulationServer.Tools.Extraction.Client;
using EmulationServer.Tools.Extraction.Extraction;
using EmulationServer.Tools.Extraction.Formats.Dbc;
using EmulationServer.Tools.Extraction.Validation;

if (args.Length == 0 || IsHelp(args[0]))
{
    PrintUsage();
    return 0;
}

try
{
    return args[0].ToLowerInvariant() switch
    {
        "builds" => PrintBuilds(),
        "dbc-info" => PrintDbcInfo(args),
        "extract-dbc" => Extract(args, AssetExtractionKind.Dbc),
        "extract-maps" => Extract(args, AssetExtractionKind.Maps),
        "extract-vmaps" => Extract(args, AssetExtractionKind.Vmaps),
        "extract-mmaps" => Extract(args, AssetExtractionKind.Mmaps),
        "extract-all" => ExtractAll(args),
        "verify-map" => VerifyMap(args),
        "verify-maps" => VerifyMaps(args),
        "formula-test" => RunFormulaTest(args),
        _ => UnknownCommand(args[0]),
    };
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Error: {exception.Message}");
    return 1;
}

static int PrintBuilds()
{
    foreach (ClientBuildInfo build in ClientBuilds.All.OrderBy(build => build.Build))
    {
        Console.WriteLine(build);
    }

    return 0;
}

static int PrintDbcInfo(string[] args)
{
    string path = RequireOption(args, "--file");
    DbcFile dbc = DbcFile.Load(path);

    Console.WriteLine($"File: {path}");
    Console.WriteLine($"Magic: {dbc.Header.Magic}");
    Console.WriteLine($"Records: {dbc.RecordCount}");
    Console.WriteLine($"Fields: {dbc.FieldCount}");
    Console.WriteLine($"Record size: {dbc.Header.RecordSize}");
    Console.WriteLine($"String block size: {dbc.Header.StringBlockSize}");

    return 0;
}

static int Extract(string[] args, AssetExtractionKind kind)
{
    AssetExtractionOptions options = CreateExtractionOptions(args);
    GameDataExtractor extractor = new();

    AssetExtractionResult result = kind switch
    {
        AssetExtractionKind.Dbc => extractor.ExtractDbc(options),
        AssetExtractionKind.Maps => extractor.ExtractMaps(options),
        AssetExtractionKind.Vmaps => extractor.ExtractVmaps(options),
        AssetExtractionKind.Mmaps => extractor.ExtractMmaps(options),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown extraction kind."),
    };

    PrintExtractionResult(result);
    return 0;
}

static int ExtractAll(string[] args)
{
    AssetExtractionOptions options = CreateExtractionOptions(args);
    GameDataExtractor extractor = new();

    foreach (AssetExtractionResult result in extractor.ExtractAll(options))
    {
        PrintExtractionResult(result);
    }

    return 0;
}

static AssetExtractionOptions CreateExtractionOptions(string[] args)
{
    string clientRoot = GetOption(args, "--client") ?? Directory.GetCurrentDirectory();
    string output = GetOption(args, "--output") ?? Path.Combine(clientRoot, "EmulationServerData");
    string locale = GetOption(args, "--locale") ?? "enUS";
    ushort build = (ushort)GetIntOption(args, "--build", ClientBuilds.Wrath335a);
    bool overwrite = !HasOption(args, "--no-overwrite");

    return new AssetExtractionOptions
    {
        ClientRootDirectory = clientRoot,
        OutputDirectory = output,
        Locale = locale,
        Build = build,
        Overwrite = overwrite,
        ProgressMessage = WriteProgressMessage,
    };
}

static void PrintExtractionResult(AssetExtractionResult result)
{
    Console.WriteLine($"[{result.Kind}] extracted={result.ExtractedFiles}, skipped={result.SkippedFiles}");
    Console.WriteLine();
}

static void WriteProgressMessage(string message)
{
    Console.WriteLine(message);
    Console.Out.Flush();
}

static int VerifyMap(string[] args)
{
    string path = RequireOption(args, "--file");
    MapDataVerifier verifier = new();
    MapValidationResult result = verifier.VerifyFile(path);
    PrintValidationResult(result);
    return result.IsValid ? 0 : 2;
}

static int VerifyMaps(string[] args)
{
    string directory = RequireOption(args, "--directory");

    if (!Directory.Exists(directory))
    {
        throw new DirectoryNotFoundException(directory);
    }

    MapDataVerifier verifier = new();
    bool valid = true;
    int checkedFiles = 0;

    foreach (string file in Directory.EnumerateFiles(directory, "*.map", SearchOption.AllDirectories).OrderBy(file => file))
    {
        checkedFiles++;
        MapValidationResult result = verifier.VerifyFile(file);
        PrintValidationResult(result);
        valid &= result.IsValid;
    }

    Console.WriteLine($"Checked {checkedFiles} map file(s).");
    return valid ? 0 : 2;
}

static int RunFormulaTest(string[] args)
{
    float gridHeight = GetFloatOption(args, "--min", -500.0f);
    float gridMaxHeight = GetFloatOption(args, "--max", 1500.0f);
    int samples = GetIntOption(args, "--samples", 10000);

    HeightFormulaVerificationResult result = HeightFormulaVerifier.Verify(gridHeight, gridMaxHeight, samples);

    Console.WriteLine($"Samples: {result.Samples}");
    Console.WriteLine($"Grid height: {result.GridHeight}");
    Console.WriteLine($"Grid max height: {result.GridMaxHeight}");
    Console.WriteLine($"UInt8 observed/expected max error: {result.UInt8MaximumObservedError:0.000000} / {result.UInt8ExpectedMaximumError:0.000000}");
    Console.WriteLine($"UInt16 observed/expected max error: {result.UInt16MaximumObservedError:0.000000} / {result.UInt16ExpectedMaximumError:0.000000}");

    return result.IsValid ? 0 : 2;
}

static void PrintValidationResult(MapValidationResult result)
{
    foreach (ValidationMessage message in result.Messages)
    {
        Console.WriteLine($"[{message.Severity}] {message.Message}");
    }
}

static int UnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command '{command}'.");
    PrintUsage();
    return 1;
}

static string RequireOption(string[] args, string name)
{
    for (int i = 1; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }

    throw new ArgumentException($"Missing required option {name}.");
}

static int GetIntOption(string[] args, string name, int defaultValue)
{
    string? value = GetOption(args, name);
    return value is null ? defaultValue : int.Parse(value);
}

static float GetFloatOption(string[] args, string name, float defaultValue)
{
    string? value = GetOption(args, name);
    return value is null ? defaultValue : float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}

static string? GetOption(string[] args, string name)
{
    for (int i = 1; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }

    return null;
}

static bool HasOption(string[] args, string name)
{
    return args.Any(argument => string.Equals(argument, name, StringComparison.OrdinalIgnoreCase));
}

static bool IsHelp(string value)
{
    return value is "-h" or "--help" or "help";
}

static void PrintUsage()
{
    Console.WriteLine("MapDataTool");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  builds");
    Console.WriteLine("      Lists supported client extraction targets.");
    Console.WriteLine();
    Console.WriteLine("  extract-dbc [--client <wow-root>] [--output <directory>] [--build <build>] [--locale <locale>] [--no-overwrite]");
    Console.WriteLine("      Extracts DBFilesClient/*.dbc from the client MPQ archives using the C# tool.");
    Console.WriteLine();
    Console.WriteLine("  extract-maps [--client <wow-root>] [--output <directory>] [--build <build>] [--locale <locale>] [--no-overwrite]");
    Console.WriteLine("      Extracts raw ADT/WDT sources and converts ADT tiles into MaNGOS-style .map files.");
    Console.WriteLine();
    Console.WriteLine("  extract-vmaps [--client <wow-root>] [--output <directory>] [--build <build>] [--locale <locale>] [--no-overwrite]");
    Console.WriteLine("      Extracts raw WMO/M2/SKIN source files from the client MPQ archives.");
    Console.WriteLine();
    Console.WriteLine("  extract-mmaps [--client <wow-root>] [--output <directory>] [--build <build>] [--locale <locale>]");
    Console.WriteLine("      Creates the mmap output location and explains that native navmesh generation is not implemented yet.");
    Console.WriteLine();
    Console.WriteLine("  extract-all [--client <wow-root>] [--output <directory>] [--build <build>] [--locale <locale>] [--no-overwrite]");
    Console.WriteLine("      Runs extract-dbc, extract-maps, extract-vmaps, and extract-mmaps.");
    Console.WriteLine();
    Console.WriteLine("  dbc-info --file <path-to-dbc>");
    Console.WriteLine("      Reads a DBC header and prints record/string block information.");
    Console.WriteLine();
    Console.WriteLine("  verify-map --file <path-to-map>");
    Console.WriteLine("      Validates one extracted .map file produced by MaNGOS-style map extraction.");
    Console.WriteLine();
    Console.WriteLine("  verify-maps --directory <path-to-maps>");
    Console.WriteLine("      Validates every .map file under a directory.");
    Console.WriteLine();
    Console.WriteLine("  formula-test [--min <height>] [--max <height>] [--samples <count>]");
    Console.WriteLine("      Verifies the uint8/uint16 height encode/decode formulas over a sampled range.");
}
