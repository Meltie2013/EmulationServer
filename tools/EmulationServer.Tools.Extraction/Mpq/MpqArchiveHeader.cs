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

internal sealed class MpqArchiveHeader
{
    private const uint MpqMagic = 0x1A51504D;

    private MpqArchiveHeader(
        long headerOffset,
        uint headerSize,
        ushort formatVersion,
        ushort blockSizePower,
        long archiveSize,
        long hashTableOffset,
        long blockTableOffset,
        uint hashTableEntries,
        uint blockTableEntries)
    {
        HeaderOffset = headerOffset;
        HeaderSize = headerSize;
        FormatVersion = formatVersion;
        BlockSizePower = blockSizePower;
        ArchiveSize = archiveSize;
        HashTableOffset = hashTableOffset;
        BlockTableOffset = blockTableOffset;
        HashTableEntries = hashTableEntries;
        BlockTableEntries = blockTableEntries;
    }

    public long HeaderOffset { get; }

    public uint HeaderSize { get; }

    public ushort FormatVersion { get; }

    public ushort BlockSizePower { get; }

    public int SectorSize => 512 << BlockSizePower;

    public long ArchiveSize { get; }

    public long HashTableOffset { get; }

    public long BlockTableOffset { get; }

    public uint HashTableEntries { get; }

    public uint BlockTableEntries { get; }

    public static MpqArchiveHeader Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        long headerOffset = FindHeaderOffset(stream);
        stream.Position = headerOffset;

        Span<byte> header = stackalloc byte[64];
        int read = stream.Read(header);

        if (read < 32)
        {
            throw new InvalidDataException("MPQ archive header is incomplete.");
        }

        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(header[0..4]);
        if (magic != MpqMagic)
        {
            throw new InvalidDataException("MPQ archive header magic was not found.");
        }

        uint headerSize = BinaryPrimitives.ReadUInt32LittleEndian(header[4..8]);
        uint archiveSize32 = BinaryPrimitives.ReadUInt32LittleEndian(header[8..12]);
        ushort formatVersion = BinaryPrimitives.ReadUInt16LittleEndian(header[12..14]);
        ushort blockSizePower = BinaryPrimitives.ReadUInt16LittleEndian(header[14..16]);
        uint hashTableOffset32 = BinaryPrimitives.ReadUInt32LittleEndian(header[16..20]);
        uint blockTableOffset32 = BinaryPrimitives.ReadUInt32LittleEndian(header[20..24]);
        uint hashTableEntries = BinaryPrimitives.ReadUInt32LittleEndian(header[24..28]);
        uint blockTableEntries = BinaryPrimitives.ReadUInt32LittleEndian(header[28..32]);

        long archiveSize = archiveSize32;
        long hashTableOffset = hashTableOffset32;
        long blockTableOffset = blockTableOffset32;

        if (formatVersion >= 1 && headerSize >= 44 && read >= 44)
        {
            ulong extendedBlockTableOffset = BinaryPrimitives.ReadUInt64LittleEndian(header[32..40]);
            ushort hashTableOffsetHigh = BinaryPrimitives.ReadUInt16LittleEndian(header[40..42]);
            ushort blockTableOffsetHigh = BinaryPrimitives.ReadUInt16LittleEndian(header[42..44]);

            if (hashTableOffsetHigh != 0)
            {
                hashTableOffset |= (long)hashTableOffsetHigh << 32;
            }

            if (blockTableOffsetHigh != 0)
            {
                blockTableOffset |= (long)blockTableOffsetHigh << 32;
            }

            if (extendedBlockTableOffset != 0)
            {
                // Extended block table entries are not needed for the known DBC files,
                // but reading this value confirms that this is a v1+ MPQ header.
            }
        }

        if (hashTableEntries == 0 || blockTableEntries == 0)
        {
            throw new InvalidDataException("MPQ archive has an empty hash or block table.");
        }

        return new MpqArchiveHeader(
            headerOffset,
            headerSize,
            formatVersion,
            blockSizePower,
            archiveSize,
            hashTableOffset,
            blockTableOffset,
            hashTableEntries,
            blockTableEntries);
    }

    private static long FindHeaderOffset(Stream stream)
    {
        const int searchStep = 0x200;
        Span<byte> magic = stackalloc byte[4];

        long originalPosition = stream.CanSeek ? stream.Position : 0;

        try
        {
            long maxSearch = stream.CanSeek ? Math.Min(stream.Length, 0x200000L) : 0x200000L;

            for (long offset = 0; offset < maxSearch; offset += searchStep)
            {
                stream.Position = offset;
                if (stream.Read(magic) != 4)
                {
                    break;
                }

                if (BinaryPrimitives.ReadUInt32LittleEndian(magic) == MpqMagic)
                {
                    return offset;
                }
            }
        }
        finally
        {
            if (stream.CanSeek)
            {
                stream.Position = originalPosition;
            }
        }

        throw new InvalidDataException("MPQ archive header was not found.");
    }
}
