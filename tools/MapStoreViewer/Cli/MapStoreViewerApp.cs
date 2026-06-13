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

using EmulationServer.Shared.Data.MapStore;
using MapStoreViewer.Parsing;
using MapStoreViewer.Rendering;
using MapStoreViewer.Scene;

namespace MapStoreViewer.Cli;

/**
  * Entry point for the standalone mapstore viewer tool.
  */
public static class MapStoreViewerApp
{
    /**
      * Runs the selected viewer command and returns the process exit code.
      */
    public static int Run(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintUsage();
            return 0;
        }

        try
        {
            string command = args[0].ToLowerInvariant();
            CommandLineOptions options = CommandLineOptions.Parse(args.Skip(1));

            return command switch
            {
                "overview" => RenderOverview(options),
                "tile" => RenderTile(options),
                "file" => RenderFile(options),
                _ => UnknownCommand(args[0]),
            };
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            Console.Error.WriteLine($"Error: {exception.Message}");
            return 1;
        }
    }

    private static int RenderOverview(CommandLineOptions options)
    {
        string mapStoreRoot = options.RequireString("mapstore");
        uint mapId = options.RequireUInt32("map");
        string outputPath = options.GetString("output", $"map_{MapStoreFileNames.FormatMapId(mapId)}_overview.html");

        bool includeOverviewPreview = options.GetBoolean("overview-preview", defaultValue: true) && !options.HasFlag("no-overview-preview");
        int overviewSampleResolution = options.GetInt32("overview-sample-resolution", 16);

        MapStoreTileLoader loader = CreateLoader(options, mapStoreRoot);
        MapOverviewScene scene = loader.LoadOverview(mapId, includeOverviewPreview, overviewSampleResolution);
        HtmlMapStoreViewerWriter.WriteOverview(outputPath, scene);
        Console.WriteLine($"Wrote mapstore overview: {outputPath}");
        return 0;
    }

    private static int RenderTile(CommandLineOptions options)
    {
        string mapStoreRoot = options.RequireString("mapstore");
        uint mapId = options.RequireUInt32("map");
        byte tileX = options.RequireByte("tile-x");
        byte tileY = options.RequireByte("tile-y");
        string outputPath = options.GetString("output", $"map_{MapStoreFileNames.FormatTileKey(mapId, tileX, tileY).Replace('/', '_')}.html");

        MapStoreTileLoader loader = CreateLoader(options, mapStoreRoot);
        MapTileScene scene = loader.LoadTile(mapId, tileX, tileY);
        HtmlMapStoreViewerWriter.WriteTile(outputPath, scene);
        Console.WriteLine($"Wrote mapstore tile preview: {outputPath}");
        return scene.Errors.Count == 0 ? 0 : 2;
    }

    private static int RenderFile(CommandLineOptions options)
    {
        string filePath = options.RequireString("file");
        string fileName = Path.GetFileName(filePath);

        if (!MapStoreFileNames.TryParseTileFileName(fileName, out byte tileX, out byte tileY, out _))
        {
            throw new ArgumentException($"'{filePath}' is not a canonical tile file name like 32_48.terrain.bin.");
        }

        string? tilesDirectory = Path.GetDirectoryName(filePath);
        string? mapDirectory = tilesDirectory is null ? null : Path.GetDirectoryName(tilesDirectory);
        string? mapsDirectory = mapDirectory is null ? null : Path.GetDirectoryName(mapDirectory);
        string? mapStoreRoot = mapsDirectory is null ? null : Path.GetDirectoryName(mapsDirectory);

        if (mapDirectory is null || mapStoreRoot is null || !MapStoreFileNames.TryParseMapDirectoryName(Path.GetFileName(mapDirectory), out uint mapId))
        {
            throw new ArgumentException("The file command expects a canonical mapstore path such as <mapstore>/maps/000/tiles/32_48.terrain.bin.");
        }

        string outputPath = options.GetString("output", $"map_{MapStoreFileNames.FormatTileKey(mapId, tileX, tileY).Replace('/', '_')}.html");
        MapStoreTileLoader loader = CreateLoader(options, mapStoreRoot);
        MapTileScene scene = loader.LoadTile(mapId, tileX, tileY);
        HtmlMapStoreViewerWriter.WriteTile(outputPath, scene);
        Console.WriteLine($"Wrote mapstore tile preview: {outputPath}");
        return scene.Errors.Count == 0 ? 0 : 2;
    }

    private static MapStoreTileLoader CreateLoader(CommandLineOptions options, string mapStoreRoot)
    {
        bool includeCollisionGeometry = options.GetBoolean("collision-geometry", defaultValue: true) && !options.HasFlag("no-collision-geometry");
        int maxCollisionTriangles = options.GetInt32("max-collision-triangles", 50000);
        string modelsRoot = options.GetString("models-root", Path.Combine(mapStoreRoot, "collision", "models"));
        return new MapStoreTileLoader(mapStoreRoot, modelsRoot, includeCollisionGeometry, maxCollisionTriangles);
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command '{command}'.");
        PrintUsage();
        return 1;
    }

    private static bool IsHelp(string value)
    {
        return string.Equals(value, "help", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "--help", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "-h", StringComparison.OrdinalIgnoreCase);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Emulation Server MapStore Viewer");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  overview --mapstore <dir> --map <id> [--output <html>]");
        Console.WriteLine("  tile     --mapstore <dir> --map <id> --tile-x <x> --tile-y <y> [--output <html>]");
        Console.WriteLine("  file     --file <canonical .bin path> [--output <html>]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --overview-preview <true|false>  Read terrain/liquid .bin files for the 2D overview; default true");
        Console.WriteLine("  --no-overview-preview            Disable the 2D terrain/liquid overview sampling");
        Console.WriteLine("  --overview-sample-resolution <n> Samples per tile side for overview; default 16, valid range 4-64");
        Console.WriteLine("  --models-root <dir>              Defaults to <mapstore>/collision/models");
        Console.WriteLine("  --collision-geometry <true|false> Render WMO collision triangles when model files exist");
        Console.WriteLine("  --no-collision-geometry          Render placement bounds only");
        Console.WriteLine("  --max-collision-triangles <n>    Safety limit for embedded WMO triangles; default 50000");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run --project tools/MapStoreViewer -- tile --mapstore ./data/mapstore --map 0 --tile-x 32 --tile-y 48 --output preview.html");
        Console.WriteLine("  dotnet run --project tools/MapStoreViewer -- overview --mapstore ./data/mapstore --map 0 --output map_000.html");
    }
}
