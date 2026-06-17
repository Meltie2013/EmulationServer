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
// File: src/EmulationServer.Shared/Data/MapStore/MapStoreTileDataFlags.cs
// Purpose: Contains map store tile data flags code for the shared infrastructure, logging, timing, and cross-service utility layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Shared.Data.MapStore;

[Flags]
// Type: MapStoreTileDataFlags
// Purpose: Defines the allowed map store tile data flags values used by the shared infrastructure, logging, timing, and cross-service utility layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public enum MapStoreTileDataFlags : byte
{
    // Enum Value: Defines the none enum value.
    // Value: explicit expression 0.
    None = 0,
    // Enum Value: Defines the terrain enum value.
    // Value: explicit expression 1 << 0.
    Terrain = 1 << 0,
    // Enum Value: Defines the liquid enum value.
    // Value: explicit expression 1 << 1.
    Liquid = 1 << 1,
    // Enum Value: Defines the collision enum value.
    // Value: explicit expression 1 << 2.
    Collision = 1 << 2,
    // Enum Value: Defines the navmesh enum value.
    // Value: explicit expression 1 << 3.
    Navmesh = 1 << 3,
}
