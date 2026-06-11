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

namespace EmulationServer.Shared.Data.MapStore;

/**
  * Reads and writes the fixed mapstore file header used by every runtime payload.
  */
public static class MapStoreBinary
{
    private static readonly uint[] Crc32Table = CreateCrc32Table();

    /**
      * Writes one complete mapstore file with header, CRC, and payload.
      */
    public static void WriteFile(string path, MapStoreDataKind kind, ushort build, uint mapId, byte tileX, byte tileY, byte[] payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(payload);

        string? parentDirectory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(parentDirectory))
        {
            Directory.CreateDirectory(parentDirectory);
        }

        uint crc32 = ComputeCrc32(payload);
        using FileStream stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using BinaryWriter writer = new(stream);

        MapStoreBinaryPrimitives.WriteFourCC(writer, MapStoreFormat.GetMagic(kind));
        writer.Write(MapStoreFormat.CurrentVersion);
        writer.Write(build);
        writer.Write(mapId);
        writer.Write(tileX);
        writer.Write(tileY);
        writer.Write((byte)kind);
        writer.Write((byte)0);
        writer.Write(checked((uint)payload.Length));
        writer.Write(crc32);
        writer.Write(payload);
    }

    /**
      * Reads and validates one complete mapstore file.
      */
    public static MapStoreFile ReadFile(string path, MapStoreDataKind expectedKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using FileStream stream = File.OpenRead(path);
        using BinaryReader reader = new(stream);

        MapStoreFileHeader header = ReadHeader(reader, path);
        ValidateHeader(header, expectedKind, stream.Length, path);

        byte[] payload = reader.ReadBytes(checked((int)header.PayloadSize));
        if ((uint)payload.Length != header.PayloadSize)
        {
            throw new InvalidDataException($"{path} ended before the declared mapstore payload could be read.");
        }

        uint actualCrc = ComputeCrc32(payload);
        if (actualCrc != header.PayloadCrc32)
        {
            throw new InvalidDataException($"{path} has invalid mapstore CRC32. Expected=0x{header.PayloadCrc32:X8}, Actual=0x{actualCrc:X8}.");
        }

        return new MapStoreFile(path, header, payload);
    }

    /**
      * Reads one fixed mapstore file header from the supplied reader.
      */
    public static MapStoreFileHeader ReadHeader(BinaryReader reader, string path)
    {
        if (reader.BaseStream.Length < MapStoreFormat.FileHeaderSize)
        {
            throw new InvalidDataException($"{path} is too small to contain a mapstore header.");
        }

        string magic = MapStoreBinaryPrimitives.ReadFourCC(reader);
        ushort version = reader.ReadUInt16();
        ushort build = reader.ReadUInt16();
        uint mapId = reader.ReadUInt32();
        byte tileX = reader.ReadByte();
        byte tileY = reader.ReadByte();
        MapStoreDataKind kind = (MapStoreDataKind)reader.ReadByte();
        _ = reader.ReadByte();
        uint payloadSize = reader.ReadUInt32();
        uint payloadCrc32 = reader.ReadUInt32();

        return new MapStoreFileHeader(magic, version, build, mapId, tileX, tileY, kind, payloadSize, payloadCrc32);
    }

    /**
      * Computes a standard CRC32 for a mapstore payload.
      */
    public static uint ComputeCrc32(ReadOnlySpan<byte> payload)
    {
        uint crc = 0xFFFFFFFFu;
        foreach (byte value in payload)
        {
            crc = (crc >> 8) ^ Crc32Table[(int)((crc ^ value) & 0xFF)];
        }

        return ~crc;
    }

    /**
      * Validates that the supplied header matches the expected file kind and physical file size.
      */
    private static void ValidateHeader(MapStoreFileHeader header, MapStoreDataKind expectedKind, long fileLength, string path)
    {
        string expectedMagic = MapStoreFormat.GetMagic(expectedKind);
        if (!string.Equals(header.Magic, expectedMagic, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"{path} has invalid mapstore magic '{header.Magic}'. Expected '{expectedMagic}'.");
        }

        if (header.Version != MapStoreFormat.CurrentVersion)
        {
            throw new InvalidDataException($"{path} has unsupported mapstore version {header.Version}. Expected {MapStoreFormat.CurrentVersion}.");
        }

        if (header.Kind != expectedKind)
        {
            throw new InvalidDataException($"{path} has invalid mapstore kind {header.Kind}. Expected {expectedKind}.");
        }

        long expectedLength = checked(MapStoreFormat.FileHeaderSize + (long)header.PayloadSize);
        if (expectedLength != fileLength)
        {
            throw new InvalidDataException($"{path} has invalid mapstore length. Header declares {header.PayloadSize} payload byte(s), file length is {fileLength} byte(s).");
        }
    }

    /**
      * Builds the CRC32 lookup table once for repeated mapstore reads and writes.
      */
    private static uint[] CreateCrc32Table()
    {
        uint[] table = new uint[256];
        for (uint i = 0; i < table.Length; i++)
        {
            uint crc = i;
            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
            }

            table[i] = crc;
        }

        return table;
    }
}
