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
using MapStoreViewer.Scene;

namespace MapStoreViewer.Parsing;

/**
  * Loads mapstore files into standalone viewer scenes without depending on the emulator runtime projects.
  */
public sealed class MapStoreTileLoader
{
    private readonly string mapStoreRoot;
    private readonly string modelsRoot;
    private readonly bool includeCollisionGeometry;
    private readonly int maxCollisionTriangles;
    private readonly Dictionary<string, CollisionModelReadResult> modelCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> missingModelKeys = new(StringComparer.OrdinalIgnoreCase);

    public MapStoreTileLoader(string mapStoreRoot, string modelsRoot, bool includeCollisionGeometry, int maxCollisionTriangles)
    {
        this.mapStoreRoot = !string.IsNullOrWhiteSpace(mapStoreRoot) ? mapStoreRoot : throw new ArgumentException("Mapstore root is required.", nameof(mapStoreRoot));
        this.modelsRoot = !string.IsNullOrWhiteSpace(modelsRoot) ? modelsRoot : throw new ArgumentException("Models root is required.", nameof(modelsRoot));
        this.includeCollisionGeometry = includeCollisionGeometry;
        this.maxCollisionTriangles = maxCollisionTriangles;
    }

    public MapTileScene LoadTile(uint mapId, byte tileX, byte tileY)
    {
        MapTileScene scene = new(mapId, tileX, tileY);
        LoadComponent(scene, MapStoreDataKind.Terrain, file => scene.Terrain = MapStorePayloadReaders.ReadTerrain(file));
        LoadComponent(scene, MapStoreDataKind.Liquid, file => scene.Liquid = MapStorePayloadReaders.ReadLiquid(file));
        LoadComponent(scene, MapStoreDataKind.Collision, file => scene.Collision = BuildCollisionScene(MapStorePayloadReaders.ReadCollisionPlacements(file)));
        LoadComponent(scene, MapStoreDataKind.Navmesh, file => scene.Navmesh = MapStorePayloadReaders.ReadNavmesh(file));
        return scene;
    }

    public MapOverviewScene LoadOverview(uint mapId, bool includePreview = true, int previewResolution = 16)
    {
        previewResolution = Math.Clamp(previewResolution, 4, 64);
        string indexPath = MapStoreFileNames.GetIndexPath(mapStoreRoot, mapId);
        List<string> messages = [];
        Dictionary<(byte X, byte Y), MapStoreTileDataFlags> tileFlags = [];
        ushort build = 0;

        if (File.Exists(indexPath))
        {
            try
            {
                (build, tileFlags) = ReadIndex(indexPath, mapId);
                messages.Add($"Loaded map index: {indexPath}");
            }
            catch (Exception exception) when (exception is InvalidDataException or IOException or UnauthorizedAccessException)
            {
                messages.Add($"Failed to read map index '{indexPath}': {exception.Message}. Falling back to tile directory scan.");
            }
        }
        else
        {
            messages.Add($"Map index was not found: {indexPath}. Falling back to tile directory scan.");
        }

        string tilesDirectory = MapStoreFileNames.GetTilesDirectory(mapStoreRoot, mapId);
        if (Directory.Exists(tilesDirectory))
        {
            foreach (string tilePath in Directory.EnumerateFiles(tilesDirectory, "*.bin", SearchOption.TopDirectoryOnly))
            {
                if (!MapStoreFileNames.TryParseTileFileName(Path.GetFileName(tilePath), out byte tileX, out byte tileY, out MapStoreDataKind kind))
                {
                    continue;
                }

                (byte X, byte Y) key = (tileX, tileY);
                tileFlags.TryGetValue(key, out MapStoreTileDataFlags flags);
                tileFlags[key] = flags | MapStoreFormat.GetTileDataFlag(kind);
            }
        }

        List<MapOverviewTileScene> tiles = [];
        int previewErrorCount = 0;
        foreach (KeyValuePair<(byte X, byte Y), MapStoreTileDataFlags> pair in tileFlags.OrderBy(static pair => pair.Key.Y).ThenBy(static pair => pair.Key.X))
        {
            byte tileX = pair.Key.X;
            byte tileY = pair.Key.Y;
            MapOverviewTilePreviewScene? preview = null;
            if (includePreview)
            {
                preview = TryBuildOverviewPreview(mapId, tileX, tileY, previewResolution, messages, ref previewErrorCount);
            }

            tiles.Add(new MapOverviewTileScene(
                tileX,
                tileY,
                pair.Value,
                GetTileLength(mapId, tileX, tileY, MapStoreDataKind.Terrain),
                GetTileLength(mapId, tileX, tileY, MapStoreDataKind.Liquid),
                GetTileLength(mapId, tileX, tileY, MapStoreDataKind.Collision),
                GetTileLength(mapId, tileX, tileY, MapStoreDataKind.Navmesh),
                preview));
        }

        if (!includePreview)
        {
            messages.Add("2D terrain/liquid overview preview was disabled by command line option.");
        }
        else if (previewErrorCount > 20)
        {
            messages.Add($"Suppressed {previewErrorCount - 20} additional overview preview read error(s).");
        }

        return new MapOverviewScene(mapId, build, previewResolution, tiles, messages);
    }


    private MapOverviewTilePreviewScene? TryBuildOverviewPreview(uint mapId, byte tileX, byte tileY, int previewResolution, List<string> messages, ref int previewErrorCount)
    {
        float[]? terrainHeights = null;
        int[]? holeMask = null;
        int[]? liquidMask = null;
        float minimumHeight = 0.0f;
        float maximumHeight = 0.0f;
        bool hasTerrain = false;
        bool hasLiquid = false;
        bool hasHoles = false;

        string terrainPath = MapStoreFileNames.GetTileFilePath(mapStoreRoot, mapId, tileX, tileY, MapStoreDataKind.Terrain);
        if (File.Exists(terrainPath))
        {
            try
            {
                TerrainScene terrain = MapStorePayloadReaders.ReadTerrain(MapStoreBinary.ReadFile(terrainPath, MapStoreDataKind.Terrain));
                terrainHeights = SampleTerrain(terrain, previewResolution);
                holeMask = BuildHoleMask(terrain, previewResolution);
                minimumHeight = terrainHeights.Min();
                maximumHeight = terrainHeights.Max();
                hasTerrain = terrain.HasHeightGrid;
                hasHoles = holeMask.Any(static value => value != 0);
            }
            catch (Exception exception) when (exception is InvalidDataException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                AddLimitedPreviewError(messages, ref previewErrorCount, mapId, tileX, tileY, MapStoreDataKind.Terrain, exception.Message);
            }
        }

        string liquidPath = MapStoreFileNames.GetTileFilePath(mapStoreRoot, mapId, tileX, tileY, MapStoreDataKind.Liquid);
        if (File.Exists(liquidPath))
        {
            try
            {
                LiquidScene liquid = MapStorePayloadReaders.ReadLiquid(MapStoreBinary.ReadFile(liquidPath, MapStoreDataKind.Liquid));
                liquidMask = BuildLiquidMask(liquid, previewResolution);
                hasLiquid = liquidMask.Any(static value => value != 0);
            }
            catch (Exception exception) when (exception is InvalidDataException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                AddLimitedPreviewError(messages, ref previewErrorCount, mapId, tileX, tileY, MapStoreDataKind.Liquid, exception.Message);
            }
        }

        if (terrainHeights is null && liquidMask is null && holeMask is null)
        {
            return null;
        }

        return new MapOverviewTilePreviewScene(terrainHeights, liquidMask, holeMask, minimumHeight, maximumHeight, hasTerrain, hasLiquid, hasHoles);
    }

    private static void AddLimitedPreviewError(List<string> messages, ref int previewErrorCount, uint mapId, byte tileX, byte tileY, MapStoreDataKind kind, string error)
    {
        previewErrorCount++;
        if (previewErrorCount <= 20)
        {
            string tileKey = MapStoreFileNames.FormatTileKey(mapId, tileX, tileY);
            messages.Add($"Could not read {kind} preview for {tileKey}: {error}");
        }
    }

    private static float[] SampleTerrain(TerrainScene terrain, int previewResolution)
    {
        float[] samples = new float[previewResolution * previewResolution];
        float[]? heights = terrain.V9Heights;
        if (heights is null || heights.Length != MapStorePayloadConstants.V9VertexCount)
        {
            Array.Fill(samples, terrain.GridHeight);
            return samples;
        }

        const int sourceSize = MapStorePayloadConstants.GridSize + 1;
        for (int y = 0; y < previewResolution; y++)
        {
            int sourceY = previewResolution == 1 ? 0 : (int)Math.Round(y * (sourceSize - 1) / (double)(previewResolution - 1));
            sourceY = Math.Clamp(sourceY, 0, sourceSize - 1);
            for (int x = 0; x < previewResolution; x++)
            {
                int sourceX = previewResolution == 1 ? 0 : (int)Math.Round(x * (sourceSize - 1) / (double)(previewResolution - 1));
                sourceX = Math.Clamp(sourceX, 0, sourceSize - 1);
                samples[(y * previewResolution) + x] = heights[(sourceY * sourceSize) + sourceX];
            }
        }

        return samples;
    }

    private static int[] BuildHoleMask(TerrainScene terrain, int previewResolution)
    {
        int[] mask = new int[previewResolution * previewResolution];
        if (!terrain.HasHoles || terrain.Holes.Length == 0)
        {
            return mask;
        }

        for (int y = 0; y < previewResolution; y++)
        {
            int cellY = Math.Min(MapStorePayloadConstants.CellsPerGrid - 1, y * MapStorePayloadConstants.CellsPerGrid / previewResolution);
            for (int x = 0; x < previewResolution; x++)
            {
                int cellX = Math.Min(MapStorePayloadConstants.CellsPerGrid - 1, x * MapStorePayloadConstants.CellsPerGrid / previewResolution);
                int cellIndex = (cellY * MapStorePayloadConstants.CellsPerGrid) + cellX;
                mask[(y * previewResolution) + x] = terrain.Holes[cellIndex] == 0 ? 0 : 1;
            }
        }

        return mask;
    }

    private static int[] BuildLiquidMask(LiquidScene liquid, int previewResolution)
    {
        int[] mask = new int[previewResolution * previewResolution];
        if (!liquid.HasLiquid || liquid.Width == 0 || liquid.Height == 0)
        {
            return mask;
        }

        int minX = liquid.OffsetX;
        int minY = liquid.OffsetY;
        int maxX = Math.Min(MapStorePayloadConstants.CellsPerGrid, minX + liquid.Width);
        int maxY = Math.Min(MapStorePayloadConstants.CellsPerGrid, minY + liquid.Height);
        for (int y = 0; y < previewResolution; y++)
        {
            int cellY = Math.Min(MapStorePayloadConstants.CellsPerGrid - 1, y * MapStorePayloadConstants.CellsPerGrid / previewResolution);
            for (int x = 0; x < previewResolution; x++)
            {
                int cellX = Math.Min(MapStorePayloadConstants.CellsPerGrid - 1, x * MapStorePayloadConstants.CellsPerGrid / previewResolution);
                if (cellX < minX || cellX >= maxX || cellY < minY || cellY >= maxY)
                {
                    continue;
                }

                int cellIndex = (cellY * MapStorePayloadConstants.CellsPerGrid) + cellX;
                bool active = true;
                byte[]? liquidFlags = liquid.LiquidFlags;
                if (liquidFlags?.Length == MapStorePayloadConstants.AreaCellCount)
                {
                    active = liquidFlags[cellIndex] != 0;
                }

                ushort[]? liquidTypeIds = liquid.LiquidTypeIds;
                if (liquidTypeIds?.Length == MapStorePayloadConstants.AreaCellCount && liquidTypeIds[cellIndex] != MapStorePayloadConstants.MapLiquidTypeNoWater)
                {
                    active = true;
                }

                if (active)
                {
                    mask[(y * previewResolution) + x] = 1;
                }
            }
        }

        return mask;
    }

    private void LoadComponent(MapTileScene scene, MapStoreDataKind kind, Action<MapStoreFile> apply)
    {
        string path = MapStoreFileNames.GetTileFilePath(mapStoreRoot, scene.MapId, scene.TileX, scene.TileY, kind);
        bool exists = File.Exists(path);
        long fileSize = exists ? new FileInfo(path).Length : 0;

        if (!exists)
        {
            scene.Components.Add(new ComponentStatusScene(kind.ToString(), path, Exists: false, Loaded: false, fileSize, Error: null));
            return;
        }

        try
        {
            MapStoreFile file = MapStoreBinary.ReadFile(path, kind);
            apply(file);
            scene.Components.Add(new ComponentStatusScene(kind.ToString(), path, Exists: true, Loaded: true, fileSize, Error: null));
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            string message = $"{kind}: {exception.Message}";
            scene.Errors.Add(message);
            scene.Components.Add(new ComponentStatusScene(kind.ToString(), path, Exists: true, Loaded: false, fileSize, message));
        }
    }

    private CollisionScene BuildCollisionScene((ushort Build, uint Version, IReadOnlyList<CollisionPlacementScene> Placements) source)
    {
        Dictionary<string, CollisionModelScene> modelSummaries = new(StringComparer.OrdinalIgnoreCase);
        List<CollisionGeometryInstanceScene> geometryInstances = [];
        List<CollisionPlacementScene> placements = [];
        int missingModelCount = 0;
        int skippedGeometryInstances = 0;
        int embeddedTriangleCount = 0;

        foreach (CollisionPlacementScene placement in source.Placements)
        {
            CollisionModelReadResult? model = TryReadCollisionModel(placement.ModelKey, placement.NormalizedPath);
            bool modelLoaded = model?.Model.Loaded is true;
            if (model is not null)
            {
                modelSummaries[placement.ModelKey] = model.Model;
                if (!model.Model.Loaded)
                {
                    missingModelCount++;
                }
            }
            else
            {
                missingModelCount++;
                if (!modelSummaries.ContainsKey(placement.ModelKey))
                {
                    modelSummaries[placement.ModelKey] = new CollisionModelScene(placement.ModelKey, placement.NormalizedPath, 0, 0, BoundsScene.Empty, 0, 0, 0, Loaded: false, Error: "Model file was not found or could not be read.");
                }
            }

            placements.Add(placement with { ModelLoaded = modelLoaded });

            if (!includeCollisionGeometry || model?.Model.Loaded is not true)
            {
                continue;
            }

            int placementTriangles = model.Model.TriangleCount;
            if (embeddedTriangleCount + placementTriangles > maxCollisionTriangles)
            {
                skippedGeometryInstances++;
                continue;
            }

            geometryInstances.Add(new CollisionGeometryInstanceScene(
                placement.ModelKey,
                placement.NormalizedPath,
                placement.UniqueId,
                placement.Position,
                placement.Rotation,
                placement.Bounds,
                model.Groups));
            embeddedTriangleCount += placementTriangles;
        }

        return new CollisionScene(
            source.Build,
            source.Version,
            placements,
            [.. modelSummaries.Values.OrderBy(static model => model.NormalizedPath, StringComparer.OrdinalIgnoreCase)],
            geometryInstances,
            modelSummaries.Values.Count(static model => model.Loaded),
            missingModelCount,
            skippedGeometryInstances,
            embeddedTriangleCount);
    }

    private CollisionModelReadResult? TryReadCollisionModel(string modelKey, string normalizedPath)
    {
        if (modelCache.TryGetValue(modelKey, out CollisionModelReadResult? cachedModel))
        {
            return cachedModel;
        }

        if (missingModelKeys.Contains(modelKey))
        {
            return null;
        }

        string modelPath = Path.Combine(modelsRoot, modelKey + ".collisionmodel.bin");
        if (!File.Exists(modelPath))
        {
            missingModelKeys.Add(modelKey);
            return null;
        }

        try
        {
            CollisionModelReadResult model = VmapModelFileReader.Read(modelPath, modelKey);
            modelCache[modelKey] = model;
            return model;
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            modelCache[modelKey] = new CollisionModelReadResult(
                new CollisionModelScene(modelKey, normalizedPath, 0, 0, BoundsScene.Empty, 0, 0, 0, Loaded: false, Error: exception.Message),
                []);
            return modelCache[modelKey];
        }
    }

    private long GetTileLength(uint mapId, byte tileX, byte tileY, MapStoreDataKind kind)
    {
        string path = MapStoreFileNames.GetTileFilePath(mapStoreRoot, mapId, tileX, tileY, kind);
        return File.Exists(path) ? new FileInfo(path).Length : 0;
    }

    private static (ushort Build, Dictionary<(byte X, byte Y), MapStoreTileDataFlags> Records) ReadIndex(string path, uint expectedMapId)
    {
        using FileStream stream = File.OpenRead(path);
        using BinaryReader reader = new(stream);

        string magic = MapStoreBinaryPrimitives.ReadFourCC(reader, "map index magic");
        if (!string.Equals(magic, MapStoreFormat.IndexMagic, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"{path} has invalid mapstore index magic '{magic}'. Expected '{MapStoreFormat.IndexMagic}'.");
        }

        ushort version = reader.ReadUInt16();
        if (version != MapStoreFormat.CurrentVersion)
        {
            throw new InvalidDataException($"{path} has unsupported mapstore index version {version}. Expected {MapStoreFormat.CurrentVersion}.");
        }

        ushort build = reader.ReadUInt16();
        uint mapId = reader.ReadUInt32();
        if (mapId != expectedMapId)
        {
            throw new InvalidDataException($"{path} belongs to map {mapId:D3}, but map {expectedMapId:D3} was requested.");
        }

        int recordCount = reader.ReadInt32();
        if (recordCount < 0)
        {
            throw new InvalidDataException($"{path} has invalid negative mapstore tile count {recordCount}.");
        }

        Dictionary<(byte X, byte Y), MapStoreTileDataFlags> records = [];
        for (int index = 0; index < recordCount; index++)
        {
            byte tileX = reader.ReadByte();
            byte tileY = reader.ReadByte();
            MapStoreTileDataFlags flags = (MapStoreTileDataFlags)reader.ReadByte();
            _ = reader.ReadByte();
            records[(tileX, tileY)] = flags;
        }

        if (stream.Position != stream.Length)
        {
            throw new InvalidDataException($"{path} contains trailing bytes after the mapstore index records.");
        }

        return (build, records);
    }
}
