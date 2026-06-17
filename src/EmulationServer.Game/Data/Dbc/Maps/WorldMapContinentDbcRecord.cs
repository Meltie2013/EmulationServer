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
// File: src/EmulationServer.Game/Data/Dbc/Maps/WorldMapContinentDbcRecord.cs
// Purpose: Contains world map continent DBC record code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Data.Dbc.Maps;

// Type: WorldMapContinentDbcRecord
// Purpose: Represents world map continent DBC record data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - Id: ID identifier used to select the exact record, object, or runtime owner.
// - MapId: Map ID identifier used to select the exact record, object, or runtime owner.
// - LeftBoundary: Left boundary value supplied by the caller for this operation.
// - RightBoundary: Right boundary value supplied by the caller for this operation.
// - TopBoundary: Top boundary value supplied by the caller for this operation.
// - BottomBoundary: Bottom boundary value supplied by the caller for this operation.
// - ContinentOffsetX: Continent offset X value supplied by the caller for this operation.
// - ContinentOffsetY: Continent offset Y value supplied by the caller for this operation.
// - Scale: Scale value supplied by the caller for this operation.
// - TaxiMinX: Taxi min X value supplied by the caller for this operation.
// - TaxiMinY: Taxi min Y value supplied by the caller for this operation.
// - TaxiMaxX: Taxi max X value supplied by the caller for this operation.
// - TaxiMaxY: Taxi max Y value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record WorldMapContinentDbcRecord(
    int Id,
    int MapId,
    int LeftBoundary,
    int RightBoundary,
    int TopBoundary,
    int BottomBoundary,
    float ContinentOffsetX,
    float ContinentOffsetY,
    float Scale,
    float TaxiMinX,
    float TaxiMinY,
    float TaxiMaxX,
    float TaxiMaxY);
