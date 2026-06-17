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
// File: src/WorldServer/Auth/WorldAuthSessionParser.cs
// Purpose: Contains world auth session parser code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.WorldServer.Networking.Packets;

namespace EmulationServer.WorldServer.Auth;

// Type: WorldAuthSessionParser
// Purpose: Provides world auth session parser behavior for the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class WorldAuthSessionParser
{

    // Method: Parse
    // Purpose: Converts incoming data into parse form for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - bytepayload: Bytepayload value supplied by the caller for this operation.
    // Returns: Returns the world auth session request value produced by this operation.
    // Notes: This keeps the operation scoped to WorldAuthSessionParser so callers do not duplicate validation, protocol, or persistence rules.
    public static WorldAuthSessionRequest Parse(byte[] payload)
    {
        WorldPacketReader reader = new(payload);

        uint clientBuild = reader.ReadUInt32();
        uint loginServerId = reader.ReadUInt32();
        string username = reader.ReadCString();
        uint clientSeed = reader.ReadUInt32();
        byte[] clientProof = reader.ReadBytes(20);
        byte[] addonInfo = reader.Remaining > 0 ? reader.ReadBytes(reader.Remaining) : [];

        return new WorldAuthSessionRequest(
            clientBuild,
            loginServerId,
            username,
            clientSeed,
            clientProof,
            addonInfo);
    }
}
