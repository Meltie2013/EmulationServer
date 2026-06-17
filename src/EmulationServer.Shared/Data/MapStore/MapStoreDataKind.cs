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
// File: src/EmulationServer.Shared/Data/MapStore/MapStoreDataKind.cs
// Purpose: Contains map store data kind code for the shared infrastructure, logging, timing, and cross-service utility layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Shared.Data.MapStore;

// Type: MapStoreDataKind
// Purpose: Defines the allowed map store data kind values used by the shared infrastructure, logging, timing, and cross-service utility layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public enum MapStoreDataKind : byte
{
    // Enum Value: Defines the terrain enum value.
    // Value: explicit expression 1.
    Terrain = 1,
    // Enum Value: Defines the liquid enum value.
    // Value: explicit expression 2.
    Liquid = 2,
    // Enum Value: Defines the collision enum value.
    // Value: explicit expression 3.
    Collision = 3,
    // Enum Value: Defines the navmesh enum value.
    // Value: explicit expression 4.
    Navmesh = 4,
}
