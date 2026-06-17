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
// File: src/EmulationServer.Game/Data/Maps/MapTileLiquidData.cs
// Purpose: Contains map tile liquid data code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Shared.Data.MapStore;

namespace EmulationServer.Game.Data.Maps;

// Type: MapTileLiquidData
// Purpose: Provides map tile liquid data behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class MapTileLiquidData
{
    // Constructor: MapTileLiquidData
    // Purpose: Initializes a new MapTileLiquidData instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - key: Key value supplied by the caller for this operation.
    // - build: Build value supplied by the caller for this operation.
    // - hasLiquid: Has liquid value supplied by the caller for this operation.
    // - flags: Flags value supplied by the caller for this operation.
    // - liquidTypeId: Liquid type ID identifier used to select the exact record, object, or runtime owner.
    // - offsetX: Offset X value supplied by the caller for this operation.
    // - offsetY: Offset Y value supplied by the caller for this operation.
    // - width: Width value supplied by the caller for this operation.
    // - height: Height value supplied by the caller for this operation.
    // - liquidLevel: Liquid level value supplied by the caller for this operation.
    // - liquidTypeIds: Liquid type ids value supplied by the caller for this operation.
    // - liquidFlags: Liquid flags value supplied by the caller for this operation.
    // - liquidHeights: Liquid heights value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapTileLiquidData so callers do not duplicate validation, protocol, or persistence rules.
    public MapTileLiquidData(
        MapTileKey key,
        ushort build,
        bool hasLiquid,
        ushort flags,
        ushort liquidTypeId,
        byte offsetX,
        byte offsetY,
        byte width,
        byte height,
        float liquidLevel,
        ushort[]? liquidTypeIds,
        byte[]? liquidFlags,
        float[]? liquidHeights)
    {
        Key = key;
        Build = build;
        HasLiquid = hasLiquid;
        Flags = flags;
        LiquidTypeId = liquidTypeId;
        OffsetX = offsetX;
        OffsetY = offsetY;
        Width = width;
        Height = height;
        LiquidLevel = liquidLevel;
        LiquidTypeIds = liquidTypeIds;
        LiquidFlags = liquidFlags;
        LiquidHeights = liquidHeights;
    }

    // Method: CreateDisabled
    // Purpose: Applies create disabled changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - key: Key value supplied by the caller for this operation.
    // Returns: Returns the map tile liquid data value produced by this operation.
    // Notes: This keeps the operation scoped to MapTileLiquidData so callers do not duplicate validation, protocol, or persistence rules.
    public static MapTileLiquidData CreateDisabled(MapTileKey key)
    {
        return new MapTileLiquidData(
            key,
            0,
            false,
            MapStorePayloadConstants.MapLiquidNoType | MapStorePayloadConstants.MapLiquidNoHeight,
            0,
            0,
            0,
            0,
            0,
            0.0f,
            null,
            null,
            null);
    }

    // Property: Gets or sets the key value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: key value exposed by the owning type.
    public MapTileKey Key { get; }
    // Property: Gets or sets the build value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: build value exposed by the owning type.
    public ushort Build { get; }
    // Property: Gets or sets the has liquid value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: has liquid value exposed by the owning type.
    public bool HasLiquid { get; }
    // Property: Gets or sets the flags value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: flags value exposed by the owning type.
    public ushort Flags { get; }
    // Property: Gets or sets the liquid type ID value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: liquid type ID value exposed by the owning type.
    public ushort LiquidTypeId { get; }
    // Property: Gets or sets the offset X value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: offset X value exposed by the owning type.
    public byte OffsetX { get; }
    // Property: Gets or sets the offset Y value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: offset Y value exposed by the owning type.
    public byte OffsetY { get; }
    // Property: Gets or sets the width value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: width value exposed by the owning type.
    public byte Width { get; }
    // Property: Gets or sets the height value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: height value exposed by the owning type.
    public byte Height { get; }
    // Property: Gets or sets the liquid level value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: liquid level value exposed by the owning type.
    public float LiquidLevel { get; }
    // Property: Gets or sets the liquid type ids value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: liquid type ids value exposed by the owning type.
    public ushort[]? LiquidTypeIds { get; }
    // Property: Gets or sets the liquid flags value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: liquid flags value exposed by the owning type.
    public byte[]? LiquidFlags { get; }
    // Property: Gets or sets the liquid heights value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: liquid heights value exposed by the owning type.
    public float[]? LiquidHeights { get; }
    // Property: Gets or sets the has liquid type grid value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: has liquid type grid value exposed by the owning type.
    public bool HasLiquidTypeGrid => LiquidTypeIds is not null;
    // Property: Gets or sets the has liquid height grid value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: has liquid height grid value exposed by the owning type.
    public bool HasLiquidHeightGrid => LiquidHeights is not null;
}
