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
// File: src/WorldServer/Networking/Packets/WorldHeaderCrypt.cs
// Purpose: Contains world header crypt code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.WorldServer.Networking.Packets;

// Type: WorldHeaderCrypt
// Purpose: Provides world header crypt behavior for the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class WorldHeaderCrypt
{

    // Field: Stores the session key state used by the world server gameplay, session, and character runtime layer.
    // Value: current session key backing value maintained by the owning type.
    private readonly byte[] _sessionKey;

    // Field: Stores the encrypt index state used by the world server gameplay, session, and character runtime layer.
    // Value: current encrypt index backing value maintained by the owning type.
    private int _encryptIndex;

    // Field: Stores the decrypt index state used by the world server gameplay, session, and character runtime layer.
    // Value: current decrypt index backing value maintained by the owning type.
    private int _decryptIndex;

    // Field: Stores the previous encrypted state used by the world server gameplay, session, and character runtime layer.
    // Value: current previous encrypted backing value maintained by the owning type.
    private byte _previousEncrypted;

    // Field: Stores the previous decrypted state used by the world server gameplay, session, and character runtime layer.
    // Value: current previous decrypted backing value maintained by the owning type.
    private byte _previousDecrypted;

    // Constructor: WorldHeaderCrypt
    // Purpose: Initializes a new WorldHeaderCrypt instance with dependencies and values required by the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - sessionKey: Session key value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldHeaderCrypt so callers do not duplicate validation, protocol, or persistence rules.
    public WorldHeaderCrypt(ReadOnlySpan<byte> sessionKey)
    {
        if (sessionKey.Length == 0)
        {
            throw new ArgumentException("Session key is required.");
        }

        _sessionKey = sessionKey.ToArray();
    }

    // Method: Encrypt
    // Purpose: Executes the encrypt operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - header: Header value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldHeaderCrypt so callers do not duplicate validation, protocol, or persistence rules.
    public void Encrypt(Span<byte> header)
    {
        for (int index = 0; index < header.Length; index++)
        {
            byte encrypted = unchecked((byte)((header[index] ^ _sessionKey[_encryptIndex]) + _previousEncrypted));
            _encryptIndex = (_encryptIndex + 1) % _sessionKey.Length;
            header[index] = encrypted;
            _previousEncrypted = encrypted;
        }
    }

    // Method: Decrypt
    // Purpose: Executes the decrypt operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - header: Header value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldHeaderCrypt so callers do not duplicate validation, protocol, or persistence rules.
    public void Decrypt(Span<byte> header)
    {
        for (int index = 0; index < header.Length; index++)
        {
            byte encrypted = header[index];
            header[index] = unchecked((byte)((encrypted - _previousDecrypted) ^ _sessionKey[_decryptIndex]));
            _decryptIndex = (_decryptIndex + 1) % _sessionKey.Length;
            _previousDecrypted = encrypted;
        }
    }
}
