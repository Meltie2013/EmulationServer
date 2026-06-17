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
// File: src/EmulationServer.Shared/Data/MapStore/MapStoreFile.cs
// Purpose: Contains map store file code for the shared infrastructure, logging, timing, and cross-service utility layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Shared.Data.MapStore;

// Type: MapStoreFile
// Purpose: Represents map store file data passed through the shared infrastructure, logging, timing, and cross-service utility layer.
// Constructor values:
// - Path: Path value supplied by the caller for this operation.
// - Header: Header value supplied by the caller for this operation.
// - bytePayload: Byte payload value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record MapStoreFile(string Path, MapStoreFileHeader Header, byte[] Payload);
