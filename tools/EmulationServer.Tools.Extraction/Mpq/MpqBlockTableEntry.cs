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

using System.Buffers.Binary;

namespace EmulationServer.Tools.Extraction.Mpq;

internal readonly struct MpqBlockTableEntry
{
    public MpqBlockTableEntry(uint filePosition, uint compressedSize, uint fileSize, MpqFileFlags flags)
    {
        FilePosition = filePosition;
        CompressedSize = compressedSize;
        FileSize = fileSize;
        Flags = flags;
    }

    public uint FilePosition { get; }

    public uint CompressedSize { get; }

    public uint FileSize { get; }

    public MpqFileFlags Flags { get; }

    public bool Exists => Flags.HasFlag(MpqFileFlags.Exists) && !Flags.HasFlag(MpqFileFlags.DeleteMarker);

    public static MpqBlockTableEntry Read(ReadOnlySpan<byte> data)
    {
        return new MpqBlockTableEntry(
            BinaryPrimitives.ReadUInt32LittleEndian(data[0..4]),
            BinaryPrimitives.ReadUInt32LittleEndian(data[4..8]),
            BinaryPrimitives.ReadUInt32LittleEndian(data[8..12]),
            (MpqFileFlags)BinaryPrimitives.ReadUInt32LittleEndian(data[12..16]));
    }
}
