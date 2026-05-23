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

namespace EmulationServer.WorldServer.Networking.Packets;

public sealed class WorldHeaderCrypt
{
    private readonly byte[] _sessionKey;
    private int _encryptIndex;
    private int _decryptIndex;
    private byte _previousEncrypted;
    private byte _previousDecrypted;

    public WorldHeaderCrypt(ReadOnlySpan<byte> sessionKey)
    {
        if (sessionKey.Length == 0)
        {
            throw new ArgumentException("Session key is required.", nameof(sessionKey));
        }

        _sessionKey = sessionKey.ToArray();
    }

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
