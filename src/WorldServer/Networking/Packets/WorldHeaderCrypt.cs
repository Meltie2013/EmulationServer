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
  * File overview: src/WorldServer/Networking/Packets/WorldHeaderCrypt.cs
  * Documents the WorldHeaderCrypt source file in the World of Warcraft packet opcode, reader, writer, and builder support area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.WorldServer.Networking.Packets;

/**
  * Owns the world header crypt behavior for the World of Warcraft packet opcode, reader, writer, and builder support layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class WorldHeaderCrypt
{
    /**
      * Holds the private session key state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly byte[] _sessionKey;
    /**
      * Holds the private encrypt index state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private int _encryptIndex;
    /**
      * Holds the private decrypt index state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private int _decryptIndex;
    /**
      * Holds the private previous encrypted state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private byte _previousEncrypted;
    /**
      * Holds the private previous decrypted state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private byte _previousDecrypted;

    /**
      * Initializes a new WorldHeaderCrypt instance with the dependencies required by the World of Warcraft packet opcode, reader, writer, and builder support workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: sessionKey.
      */
    public WorldHeaderCrypt(ReadOnlySpan<byte> sessionKey)
    {
        if (sessionKey.Length == 0)
        {
            throw new ArgumentException("Session key is required.");
        }

        _sessionKey = sessionKey.ToArray();
    }

    /**
      * Performs the encrypt operation for the World of Warcraft packet opcode, reader, writer, and builder support workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: header.
      */
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

    /**
      * Performs the decrypt operation for the World of Warcraft packet opcode, reader, writer, and builder support workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: header.
      */
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
