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
// File: src/EmulationServer.Shared/Data/MapStore/MapStoreFileHeader.cs
// Purpose: Contains map store file header code for the shared infrastructure, logging, timing, and cross-service utility layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Shared.Data.MapStore;

// Type: MapStoreFileHeader
// Purpose: Represents map store file header data passed through the shared infrastructure, logging, timing, and cross-service utility layer.
// Constructor values:
// - Magic: Magic value supplied by the caller for this operation.
// - Version: Version value supplied by the caller for this operation.
// - Build: Build value supplied by the caller for this operation.
// - MapId: Map ID identifier used to select the exact record, object, or runtime owner.
// - TileX: Tile X value supplied by the caller for this operation.
// - TileY: Tile Y value supplied by the caller for this operation.
// - Kind: Kind value supplied by the caller for this operation.
// - PayloadSize: Payload size value supplied by the caller for this operation.
// - PayloadCrc32: Payload crc32 value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record MapStoreFileHeader(
    string Magic,
    ushort Version,
    ushort Build,
    uint MapId,
    byte TileX,
    byte TileY,
    MapStoreDataKind Kind,
    uint PayloadSize,
    uint PayloadCrc32);
