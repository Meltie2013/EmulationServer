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
// File: src/EmulationServer.Game/Data/Maps/MapFileHeader.cs
// Purpose: Contains map file header code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Data.Maps;

// Type: MapFileHeader
// Purpose: Represents map file header data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - MapMagic: Map magic value supplied by the caller for this operation.
// - VersionMagic: Version magic value supplied by the caller for this operation.
// - Build: Build value supplied by the caller for this operation.
// - AreaMapOffset: Area map offset value supplied by the caller for this operation.
// - AreaMapSize: Area map size value supplied by the caller for this operation.
// - HeightMapOffset: Height map offset value supplied by the caller for this operation.
// - HeightMapSize: Height map size value supplied by the caller for this operation.
// - LiquidMapOffset: Liquid map offset value supplied by the caller for this operation.
// - LiquidMapSize: Liquid map size value supplied by the caller for this operation.
// - HolesOffset: Holes offset value supplied by the caller for this operation.
// - HolesSize: Holes size value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record MapFileHeader(
    string MapMagic,
    string VersionMagic,
    uint Build,
    uint AreaMapOffset,
    uint AreaMapSize,
    uint HeightMapOffset,
    uint HeightMapSize,
    uint LiquidMapOffset,
    uint LiquidMapSize,
    uint HolesOffset,
    uint HolesSize);
