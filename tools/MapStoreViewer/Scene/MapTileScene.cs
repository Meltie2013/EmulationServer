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

namespace MapStoreViewer.Scene;

/**
  * Root data passed from the .bin parsers to the HTML/WebGL renderer.
  */
public sealed class MapTileScene
{
    public MapTileScene(uint mapId, byte tileX, byte tileY)
    {
        MapId = mapId;
        TileX = tileX;
        TileY = tileY;
        TileKey = MapStoreFileNames.FormatTileKey(mapId, tileX, tileY);
    }

    public uint MapId { get; }
    public byte TileX { get; }
    public byte TileY { get; }
    public string TileKey { get; }
    public TerrainScene? Terrain { get; set; }
    public LiquidScene? Liquid { get; set; }
    public CollisionScene? Collision { get; set; }
    public NavmeshScene? Navmesh { get; set; }
    public List<ComponentStatusScene> Components { get; } = [];
    public List<string> Errors { get; } = [];
}

public sealed record ComponentStatusScene(string Kind, string Path, bool Exists, bool Loaded, long FileSize, string? Error);

public sealed record TerrainScene(
    ushort Build,
    ushort AreaFlags,
    ushort GridAreaFlag,
    uint HeightFlags,
    float GridHeight,
    float GridMaxHeight,
    float MinimumHeight,
    float MaximumHeight,
    float[]? V9Heights,
    ushort[] Holes,
    bool HasAreaGrid,
    bool HasHeightGrid,
    bool HasHoles);

public sealed record LiquidScene(
    ushort Build,
    bool HasLiquid,
    ushort Flags,
    ushort LiquidTypeId,
    byte OffsetX,
    byte OffsetY,
    byte Width,
    byte Height,
    float LiquidLevel,
    float MinimumHeight,
    float MaximumHeight,
    ushort[]? LiquidTypeIds,
    byte[]? LiquidFlags,
    float[]? LiquidHeights);

public sealed record CollisionScene(
    ushort Build,
    uint Version,
    IReadOnlyList<CollisionPlacementScene> Placements,
    IReadOnlyList<CollisionModelScene> Models,
    IReadOnlyList<CollisionGeometryInstanceScene> GeometryInstances,
    int LoadedModelCount,
    int MissingModelCount,
    int SkippedGeometryInstances,
    int EmbeddedTriangleCount);

public sealed record CollisionPlacementScene(
    string ModelKey,
    string NormalizedPath,
    uint UniqueId,
    Vector3Scene Position,
    Vector3Scene Rotation,
    BoundsScene Bounds,
    uint Flags,
    ushort DoodadSet,
    ushort NameSet,
    bool ModelLoaded);

public sealed record CollisionModelScene(
    string ModelKey,
    string NormalizedPath,
    ushort Build,
    uint Version,
    BoundsScene Bounds,
    int GroupCount,
    int VertexCount,
    int TriangleCount,
    bool Loaded,
    string? Error);

public sealed record CollisionGeometryInstanceScene(
    string ModelKey,
    string NormalizedPath,
    uint UniqueId,
    Vector3Scene Position,
    Vector3Scene Rotation,
    BoundsScene Bounds,
    IReadOnlyList<CollisionGeometryGroupScene> Groups);

public sealed record CollisionGeometryGroupScene(
    int GroupIndex,
    uint Flags,
    BoundsScene Bounds,
    float[] Vertices,
    int[] Indices);

public sealed record NavmeshScene(ushort Build, uint PolygonCount, uint ConnectionCount, bool HasNavigationData);

public readonly record struct Vector3Scene(float X, float Y, float Z)
{
    public static Vector3Scene Zero { get; } = new(0.0f, 0.0f, 0.0f);
}

public readonly record struct BoundsScene(Vector3Scene Minimum, Vector3Scene Maximum)
{
    public static BoundsScene Empty { get; } = new(Vector3Scene.Zero, Vector3Scene.Zero);
}
