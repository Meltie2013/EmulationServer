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
// File: src/EmulationServer.Shared/Data/MapStore/MapStorePayloadConstants.cs
// Purpose: Contains map store payload constants code for the shared infrastructure, logging, timing, and cross-service utility layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Shared.Data.MapStore;

// Type: MapStorePayloadConstants
// Purpose: Provides map store payload constants behavior for the shared infrastructure, logging, timing, and cross-service utility layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class MapStorePayloadConstants
{
    // Constant: Defines the terrain magic constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed terrain magic value used anywhere this rule or protocol value is needed.
    public const string TerrainMagic = "TRN1";
    // Constant: Defines the liquid payload magic constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed liquid payload magic value used anywhere this rule or protocol value is needed.
    public const string LiquidPayloadMagic = "LIQ1";
    // Constant: Defines the collision payload magic constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed collision payload magic value used anywhere this rule or protocol value is needed.
    public const string CollisionPayloadMagic = "ESVTIL1";
    // Constant: Defines the navmesh payload magic constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed navmesh payload magic value used anywhere this rule or protocol value is needed.
    public const string NavmeshPayloadMagic = "NAV1";
    // Constant: Defines the area magic constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed area magic value used anywhere this rule or protocol value is needed.
    public const string AreaMagic = "AREA";
    // Constant: Defines the height magic constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed height magic value used anywhere this rule or protocol value is needed.
    public const string HeightMagic = "MHGT";
    // Constant: Defines the liquid section magic constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed liquid section magic value used anywhere this rule or protocol value is needed.
    public const string LiquidSectionMagic = "MLIQ";
    // Constant: Defines the cells per grid constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed cells per grid value used anywhere this rule or protocol value is needed.
    public const int CellsPerGrid = 16;
    // Constant: Defines the grid size constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed grid size value used anywhere this rule or protocol value is needed.
    public const int GridSize = 128;
    // Constant: Defines the V8 vertex count constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed V8 vertex count value used anywhere this rule or protocol value is needed.
    public const int V8VertexCount = GridSize * GridSize;
    // Method: V9VertexCount
    // Purpose: Executes the V9 vertex count operation for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - GridSize: Grid size value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to MapStorePayloadConstants so callers do not duplicate validation, protocol, or persistence rules.
    public const int V9VertexCount = (GridSize + 1) * (GridSize + 1);
    // Constant: Defines the area cell count constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed area cell count value used anywhere this rule or protocol value is needed.
    public const int AreaCellCount = CellsPerGrid * CellsPerGrid;
    // Constant: Defines the map area no area constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed map area no area value used anywhere this rule or protocol value is needed.
    public const ushort MapAreaNoArea = 0x0001;
    // Constant: Defines the map height no height constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed map height no height value used anywhere this rule or protocol value is needed.
    public const uint MapHeightNoHeight = 0x0001;
    // Constant: Defines the map height as int16 constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed map height as int16 value used anywhere this rule or protocol value is needed.
    public const uint MapHeightAsInt16 = 0x0002;
    // Constant: Defines the map height as int8 constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed map height as int8 value used anywhere this rule or protocol value is needed.
    public const uint MapHeightAsInt8 = 0x0004;
    // Constant: Defines the map liquid type no water constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed map liquid type no water value used anywhere this rule or protocol value is needed.
    public const byte MapLiquidTypeNoWater = 0x00;
    // Constant: Defines the map liquid no type constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed map liquid no type value used anywhere this rule or protocol value is needed.
    public const ushort MapLiquidNoType = 0x0001;
    // Constant: Defines the map liquid no height constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed map liquid no height value used anywhere this rule or protocol value is needed.
    public const ushort MapLiquidNoHeight = 0x0002;
}
