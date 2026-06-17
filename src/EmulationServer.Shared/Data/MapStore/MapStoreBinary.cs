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
// File: src/EmulationServer.Shared/Data/MapStore/MapStoreBinary.cs
// Purpose: Contains map store binary code for the shared infrastructure, logging, timing, and cross-service utility layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Shared.Data.MapStore;

// Type: MapStoreBinary
// Purpose: Provides map store binary behavior for the shared infrastructure, logging, timing, and cross-service utility layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class MapStoreBinary
{
    // Method: CreateCrc32Table
    // Purpose: Applies create crc32 table changes for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters: none.
    // Returns: Returns the uint[] crc32 table = value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreBinary so callers do not duplicate validation, protocol, or persistence rules.
    private static readonly uint[] Crc32Table = CreateCrc32Table();

    // Method: WriteFile
    // Purpose: Builds or writes write file output for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - path: Path value supplied by the caller for this operation.
    // - kind: Kind value supplied by the caller for this operation.
    // - build: Build value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - tileX: Tile X value supplied by the caller for this operation.
    // - tileY: Tile Y value supplied by the caller for this operation.
    // - bytepayload: Bytepayload value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapStoreBinary so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadFile
    // Purpose: Retrieves read file data for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - path: Path value supplied by the caller for this operation.
    // - expectedKind: Expected kind value supplied by the caller for this operation.
    // Returns: Returns the map store file value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreBinary so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadHeader
    // Purpose: Retrieves read header data for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - reader: Database reader used to execute this operation without opening unnecessary additional state.
    // - path: Path value supplied by the caller for this operation.
    // Returns: Returns the map store file header value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreBinary so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ComputeCrc32
    // Purpose: Calculates compute crc32 values for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - payload: Payload bytes or structured payload consumed by this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreBinary so callers do not duplicate validation, protocol, or persistence rules.
    public static uint ComputeCrc32(ReadOnlySpan<byte> payload)
    {
        uint crc = 0xFFFFFFFFu;
        foreach (byte value in payload)
        {
            crc = (crc >> 8) ^ Crc32Table[(int)((crc ^ value) & 0xFF)];
        }

        return ~crc;
    }

    // Method: ValidateHeader
    // Purpose: Validates or evaluates validate header rules for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - header: Header value supplied by the caller for this operation.
    // - expectedKind: Expected kind value supplied by the caller for this operation.
    // - fileLength: File length value supplied by the caller for this operation.
    // - path: Path value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapStoreBinary so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: CreateCrc32Table
    // Purpose: Applies create crc32 table changes for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters: none.
    // Returns: Returns the uint[] value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreBinary so callers do not duplicate validation, protocol, or persistence rules.
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
