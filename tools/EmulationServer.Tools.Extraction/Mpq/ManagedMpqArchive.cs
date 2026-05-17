
using System.Buffers.Binary;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace EmulationServer.Tools.Extraction.Mpq;

internal sealed class ManagedMpqArchive : IDisposable
{
    private readonly FileStream _stream;
    private readonly MpqArchiveHeader _header;
    private readonly MpqHashTableEntry[] _hashTable;
    private readonly MpqBlockTableEntry[] _blockTable;

    private ManagedMpqArchive(
        FileStream stream,
        MpqArchiveHeader header,
        MpqHashTableEntry[] hashTable,
        MpqBlockTableEntry[] blockTable)
    {
        _stream = stream;
        _header = header;
        _hashTable = hashTable;
        _blockTable = blockTable;
    }

    public static ManagedMpqArchive Open(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        try
        {
            MpqArchiveHeader header = MpqArchiveHeader.Read(stream);
            MpqHashTableEntry[] hashTable = ReadHashTable(stream, header);
            MpqBlockTableEntry[] blockTable = ReadBlockTable(stream, header);

            return new ManagedMpqArchive(stream, header, hashTable, blockTable);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    public bool TryReadFile(string fileName, out byte[] data)
    {
        data = [];

        if (!TryFindBlock(fileName, out MpqBlockTableEntry block))
        {
            return false;
        }

        data = ReadBlockFile(fileName, block);
        return true;
    }

    public void Dispose()
    {
        _stream.Dispose();
    }

    private static MpqHashTableEntry[] ReadHashTable(Stream stream, MpqArchiveHeader header)
    {
        int tableBytesLength = checked((int)header.HashTableEntries * 16);
        byte[] tableBytes = ReadBytesAt(stream, header.HeaderOffset + header.HashTableOffset, tableBytesLength);
        MpqHash.DecryptBlock(tableBytes, MpqHash.HashString("(hash table)", MpqHashType.FileKey));

        MpqHashTableEntry[] table = new MpqHashTableEntry[checked((int)header.HashTableEntries)];

        for (int i = 0; i < table.Length; i++)
        {
            table[i] = MpqHashTableEntry.Read(tableBytes.AsSpan(i * 16, 16));
        }

        return table;
    }

    private static MpqBlockTableEntry[] ReadBlockTable(Stream stream, MpqArchiveHeader header)
    {
        int tableBytesLength = checked((int)header.BlockTableEntries * 16);
        byte[] tableBytes = ReadBytesAt(stream, header.HeaderOffset + header.BlockTableOffset, tableBytesLength);
        MpqHash.DecryptBlock(tableBytes, MpqHash.HashString("(block table)", MpqHashType.FileKey));

        MpqBlockTableEntry[] table = new MpqBlockTableEntry[checked((int)header.BlockTableEntries)];

        for (int i = 0; i < table.Length; i++)
        {
            table[i] = MpqBlockTableEntry.Read(tableBytes.AsSpan(i * 16, 16));
        }

        return table;
    }

    private bool TryFindBlock(string fileName, out MpqBlockTableEntry block)
    {
        block = default;

        string normalizedName = MpqHash.NormalizeFileName(fileName);
        uint nameHashA = MpqHash.HashString(normalizedName, MpqHashType.NameA);
        uint nameHashB = MpqHash.HashString(normalizedName, MpqHashType.NameB);
        uint tableOffset = MpqHash.HashString(normalizedName, MpqHashType.TableOffset);
        uint startIndex = tableOffset % (uint)_hashTable.Length;

        for (uint probe = 0; probe < _hashTable.Length; probe++)
        {
            uint hashIndex = (startIndex + probe) % (uint)_hashTable.Length;
            MpqHashTableEntry hashEntry = _hashTable[hashIndex];

            if (hashEntry.IsEmpty)
            {
                return false;
            }

            if (hashEntry.IsDeleted)
            {
                continue;
            }

            if (hashEntry.NameHashA != nameHashA || hashEntry.NameHashB != nameHashB)
            {
                continue;
            }

            if (hashEntry.BlockIndex >= _blockTable.Length)
            {
                continue;
            }

            MpqBlockTableEntry candidate = _blockTable[hashEntry.BlockIndex];
            if (!candidate.Exists)
            {
                continue;
            }

            block = candidate;
            return true;
        }

        return false;
    }

    private byte[] ReadBlockFile(string fileName, MpqBlockTableEntry block)
    {
        if (block.FileSize > int.MaxValue || block.CompressedSize > int.MaxValue)
        {
            throw new NotSupportedException($"File '{fileName}' is too large to extract with this tool.");
        }

        if (block.Flags.HasFlag(MpqFileFlags.Imploded))
        {
            throw new NotSupportedException($"File '{fileName}' uses PKWARE implode compression, which is not supported yet.");
        }

        long dataOffset = _header.HeaderOffset + block.FilePosition;

        if (block.Flags.HasFlag(MpqFileFlags.SingleUnit))
        {
            byte[] compressed = ReadBytesAt(_stream, dataOffset, checked((int)block.CompressedSize));

            if (block.Flags.HasFlag(MpqFileFlags.Encrypted))
            {
                MpqHash.DecryptBlock(compressed, GetFileKey(fileName, block));
            }

            return block.Flags.HasFlag(MpqFileFlags.Compressed) && block.CompressedSize < block.FileSize
                ? Decompress(compressed, checked((int)block.FileSize))
                : ResizeToFileSize(compressed, checked((int)block.FileSize));
        }

        return ReadSectorFile(fileName, block, dataOffset);
    }

    private byte[] ReadSectorFile(string fileName, MpqBlockTableEntry block, long dataOffset)
    {
        int fileSize = checked((int)block.FileSize);
        int compressedSize = checked((int)block.CompressedSize);
        int sectorSize = _header.SectorSize;
        int sectorCount = Math.Max(1, (fileSize + sectorSize - 1) / sectorSize);
        int sectorTableSize = checked((sectorCount + 1) * 4);

        byte[] sectorTableBytes = ReadBytesAt(_stream, dataOffset, sectorTableSize);

        if (block.Flags.HasFlag(MpqFileFlags.Encrypted))
        {
            MpqHash.DecryptBlock(sectorTableBytes, GetFileKey(fileName, block) - 1);
        }

        int[] sectorOffsets = new int[sectorCount + 1];

        for (int i = 0; i < sectorOffsets.Length; i++)
        {
            sectorOffsets[i] = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(sectorTableBytes.AsSpan(i * 4, 4)));
        }

        ValidateSectorOffsets(fileName, compressedSize, sectorOffsets);

        byte[] output = new byte[fileSize];
        int outputOffset = 0;

        for (int sectorIndex = 0; sectorIndex < sectorCount; sectorIndex++)
        {
            int sectorOffset = sectorOffsets[sectorIndex];
            int nextSectorOffset = sectorOffsets[sectorIndex + 1];
            int sectorCompressedSize = nextSectorOffset - sectorOffset;
            int expectedSectorSize = Math.Min(sectorSize, fileSize - outputOffset);

            if (sectorCompressedSize <= 0)
            {
                throw new InvalidDataException($"File '{fileName}' has an invalid MPQ sector size.");
            }

            byte[] sectorData = ReadBytesAt(_stream, dataOffset + sectorOffset, sectorCompressedSize);

            if (block.Flags.HasFlag(MpqFileFlags.Encrypted))
            {
                MpqHash.DecryptBlock(sectorData, GetFileKey(fileName, block) + (uint)sectorIndex);
            }

            byte[] decodedSector = block.Flags.HasFlag(MpqFileFlags.Compressed) && sectorCompressedSize < expectedSectorSize
                ? Decompress(sectorData, expectedSectorSize)
                : ResizeToFileSize(sectorData, expectedSectorSize);

            Buffer.BlockCopy(decodedSector, 0, output, outputOffset, expectedSectorSize);
            outputOffset += expectedSectorSize;
        }

        return output;
    }

    private static void ValidateSectorOffsets(string fileName, int compressedSize, IReadOnlyList<int> sectorOffsets)
    {
        if (sectorOffsets.Count < 2)
        {
            throw new InvalidDataException($"File '{fileName}' has an invalid sector table.");
        }

        int previous = 0;

        foreach (int offset in sectorOffsets)
        {
            if (offset < previous || offset < 0 || offset > compressedSize)
            {
                throw new InvalidDataException($"File '{fileName}' has invalid MPQ sector offsets.");
            }

            previous = offset;
        }
    }

    private static uint GetFileKey(string fileName, MpqBlockTableEntry block)
    {
        string normalizedName = MpqHash.NormalizeFileName(fileName);
        int separatorIndex = normalizedName.LastIndexOf('\\');
        string baseName = separatorIndex >= 0 ? normalizedName[(separatorIndex + 1)..] : normalizedName;
        uint key = MpqHash.HashString(baseName, MpqHashType.FileKey);

        if (block.Flags.HasFlag(MpqFileFlags.FixKey))
        {
            key = (key + block.FilePosition) ^ block.FileSize;
        }

        return key;
    }

    private static byte[] Decompress(byte[] data, int expectedSize)
    {
        if (data.Length == expectedSize)
        {
            return data;
        }

        if (data.Length == 0)
        {
            return [];
        }

        byte compressionMask = data[0];
        byte[] compressedPayload = data[1..];

        if ((compressionMask & 0x10) != 0)
        {
            return DecompressBZip2(compressedPayload, expectedSize);
        }

        if ((compressionMask & 0x02) != 0)
        {
            return DecompressDeflate(compressedPayload, expectedSize);
        }

        if ((compressionMask & 0x08) != 0)
        {
            throw new NotSupportedException("MPQ PKWARE implode compression is not supported yet.");
        }

        if ((compressionMask & 0x01) != 0)
        {
            throw new NotSupportedException("MPQ Huffman compression is not supported yet.");
        }

        throw new NotSupportedException($"Unsupported MPQ compression mask 0x{compressionMask:X2}.");
    }

    private static byte[] DecompressDeflate(byte[] data, int expectedSize)
    {
        using MemoryStream input = new(data);
        using InflaterInputStream inflater = new(input);
        using MemoryStream output = new(expectedSize);
        inflater.CopyTo(output);
        return ResizeToFileSize(output.ToArray(), expectedSize);
    }

    private static byte[] DecompressBZip2(byte[] data, int expectedSize)
    {
        using MemoryStream input = new(data);
        using BZip2InputStream bzip2 = new(input);
        using MemoryStream output = new(expectedSize);
        bzip2.CopyTo(output);
        return ResizeToFileSize(output.ToArray(), expectedSize);
    }

    private static byte[] ResizeToFileSize(byte[] data, int fileSize)
    {
        if (data.Length == fileSize)
        {
            return data;
        }

        if (data.Length < fileSize)
        {
            throw new InvalidDataException($"Decoded MPQ file was smaller than expected. Expected {fileSize} byte(s), got {data.Length}.");
        }

        byte[] resized = new byte[fileSize];
        Buffer.BlockCopy(data, 0, resized, 0, fileSize);
        return resized;
    }

    private static byte[] ReadBytesAt(Stream stream, long offset, int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        byte[] data = new byte[count];
        stream.Position = offset;
        int totalRead = 0;

        while (totalRead < count)
        {
            int read = stream.Read(data, totalRead, count - totalRead);
            if (read == 0)
            {
                throw new EndOfStreamException("Unexpected end of MPQ archive.");
            }

            totalRead += read;
        }

        return data;
    }
}
