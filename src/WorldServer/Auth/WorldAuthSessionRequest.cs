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

/**
 * File overview: src/WorldServer/Auth/WorldAuthSessionRequest.cs
 * Documents the WorldAuthSessionRequest source file in the world authentication parsing and session key handling area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.WorldServer.Auth;

/**
 * Carries immutable world auth session request data for the world authentication parsing and session key handling layer.
 * Records in this project are used as explicit transfer models so packet parsing, database repositories, and runtime systems can pass strongly typed values without mutating shared state.
 * Positional fields carried by this record: ClientBuild, LoginServerId, Username, ClientSeed, ClientProof, AddonInfo.
 */
public sealed record WorldAuthSessionRequest(
    uint ClientBuild,
    uint LoginServerId,
    string Username,
    uint ClientSeed,
    byte[] ClientProof,
    byte[] AddonInfo);
