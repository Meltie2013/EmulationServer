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
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

/**
  * File overview: tools/EmulationServer.Tools.Extraction/Mpq/ManagedMpqArchive.cs
  * Documents the ManagedMpqArchive source file in the client data extraction and conversion tooling area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Tools.Extraction.Mpq;

/**
  * Owns the managed mpq archive behavior for the client data extraction and conversion tooling layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
internal sealed class ManagedMpqArchive : IDisposable
{
    /**
      * Holds the private stream state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly FileStream _stream;
    /**
      * Holds the private header state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly MpqArchiveHeader _header;
    /**
      * Holds the private hash table state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly MpqHashTableEntry[] _hashTable;
    /**
      * Holds the private block table state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly MpqBlockTableEntry[] _blockTable;

    /**
      * Initializes a new ManagedMpqArchive instance with the dependencies required by the client data extraction and conversion tooling workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: stream, header, hashTable, blockTable.
      */
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

    /**
      * Performs the open operation for the client data extraction and conversion tooling workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: path.
      */
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

    /**
      * Attempts the operation without treating a normal failure as an exceptional condition.
      * The method is part of ManagedMpqArchive and keeps this workflow isolated from the caller.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
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

    /**
      * Stops the dispose workflow and releases owned runtime resources in a controlled order.
      * Shutdown logic is centralized to avoid dangling connections, incomplete saves, or partially registered services.
      */
    public void Dispose()
    {
        _stream.Dispose();
    }

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of ManagedMpqArchive and keeps this workflow isolated from the caller.
      */
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

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of ManagedMpqArchive and keeps this workflow isolated from the caller.
      */
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

    /**
      * Attempts the operation without treating a normal failure as an exceptional condition.
      * The method is part of ManagedMpqArchive and keeps this workflow isolated from the caller.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
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

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of ManagedMpqArchive and keeps this workflow isolated from the caller.
      */
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

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of ManagedMpqArchive and keeps this workflow isolated from the caller.
      */
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

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of ManagedMpqArchive and keeps this workflow isolated from the caller.
      */
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

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of ManagedMpqArchive and keeps this workflow isolated from the caller.
      */
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

    /**
      * Performs the decompress operation for the client data extraction and conversion tooling workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: data, expectedSize.
      */
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

    /**
      * Performs the decompress deflate operation for the client data extraction and conversion tooling workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: data, expectedSize.
      */
    private static byte[] DecompressDeflate(byte[] data, int expectedSize)
    {
        using MemoryStream input = new(data);
        using InflaterInputStream inflater = new(input);
        using MemoryStream output = new(expectedSize);
        inflater.CopyTo(output);
        return ResizeToFileSize(output.ToArray(), expectedSize);
    }

    /**
      * Performs the decompress b zip 2 operation for the client data extraction and conversion tooling workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: data, expectedSize.
      */
    private static byte[] DecompressBZip2(byte[] data, int expectedSize)
    {
        using MemoryStream input = new(data);
        using BZip2InputStream bzip2 = new(input);
        using MemoryStream output = new(expectedSize);
        bzip2.CopyTo(output);
        return ResizeToFileSize(output.ToArray(), expectedSize);
    }

    /**
      * Performs the resize to file size operation for the client data extraction and conversion tooling workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: data, fileSize.
      */
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

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of ManagedMpqArchive and keeps this workflow isolated from the caller.
      */
    private static byte[] ReadBytesAt(Stream stream, long offset, int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException();
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
