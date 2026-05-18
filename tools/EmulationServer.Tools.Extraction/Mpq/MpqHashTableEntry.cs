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

internal readonly struct MpqHashTableEntry
{
    public MpqHashTableEntry(uint nameHashA, uint nameHashB, ushort locale, ushort platform, uint blockIndex)
    {
        NameHashA = nameHashA;
        NameHashB = nameHashB;
        Locale = locale;
        Platform = platform;
        BlockIndex = blockIndex;
    }

    public uint NameHashA { get; }

    public uint NameHashB { get; }

    public ushort Locale { get; }

    public ushort Platform { get; }

    public uint BlockIndex { get; }

    public bool IsEmpty => BlockIndex == 0xFFFFFFFF;

    public bool IsDeleted => BlockIndex == 0xFFFFFFFE;

    public static MpqHashTableEntry Read(ReadOnlySpan<byte> data)
    {
        return new MpqHashTableEntry(
            BinaryPrimitives.ReadUInt32LittleEndian(data[0..4]),
            BinaryPrimitives.ReadUInt32LittleEndian(data[4..8]),
            BinaryPrimitives.ReadUInt16LittleEndian(data[8..10]),
            BinaryPrimitives.ReadUInt16LittleEndian(data[10..12]),
            BinaryPrimitives.ReadUInt32LittleEndian(data[12..16]));
    }
}
