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

using System.Text;


/**
 * File overview: tools/EmulationServer.Tools.Extraction/Formats/Maps/ExtractedMapFileReader.cs
 * Documents the ExtractedMapFileReader source file in the client data extraction and conversion tooling area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Tools.Extraction.Formats.Maps;

/**
 * Owns the extracted map file reader behavior for the client data extraction and conversion tooling layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public static class ExtractedMapFileReader
{
    /**
     * Defines the constant value for map file header size.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    private const int MapFileHeaderSize = MapFormatConstants.MapFileHeaderSize;
    /**
     * Defines the constant value for area header size.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    private const int AreaHeaderSize = 8;
    /**
     * Defines the constant value for height header size.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    private const int HeightHeaderSize = 16;
    /**
     * Defines the constant value for liquid header size.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    private const int LiquidHeaderSize = 16;

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of ExtractedMapFileReader and keeps this workflow isolated from the caller.
      */
    public static ExtractedMapFile Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using FileStream stream = File.OpenRead(path);
        using BinaryReader reader = new(stream, Encoding.ASCII, leaveOpen: false);

        MapFileHeader header = ReadHeader(reader);
        ValidateHeader(header, stream.Length, path);

        MapAreaSection? area = ReadAreaSection(reader, header);
        MapHeightSection? height = ReadHeightSection(reader, header);
        MapLiquidSection? liquid = ReadLiquidSection(reader, header);

        return new ExtractedMapFile(path, header, area, height, liquid, checked((int)header.HolesSize));
    }

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of ExtractedMapFileReader and keeps this workflow isolated from the caller.
      */
    private static MapFileHeader ReadHeader(BinaryReader reader)
    {
        return new MapFileHeader(
            ReadFourCC(reader),
            ReadFourCC(reader),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32());
    }

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of ExtractedMapFileReader and keeps this workflow isolated from the caller.
      */
    private static MapAreaSection? ReadAreaSection(BinaryReader reader, MapFileHeader header)
    {
        if (header.AreaMapOffset == 0 || header.AreaMapSize == 0)
        {
            return null;
        }

        reader.BaseStream.Position = header.AreaMapOffset;
        string fourcc = ReadFourCC(reader);

        if (!string.Equals(fourcc, MapFormatConstants.AreaMagic, StringComparison.Ordinal))
        {
            throw new MapFormatException($"Invalid map area magic '{fourcc}'. Expected '{MapFormatConstants.AreaMagic}'.");
        }

        ushort flags = reader.ReadUInt16();
        ushort gridArea = reader.ReadUInt16();
        List<ushort> areaFlags = [];

        if ((flags & MapFormatConstants.MapAreaNoArea) == 0)
        {
            for (int i = 0; i < MapFormatConstants.AreaCellCount; i++)
            {
                areaFlags.Add(reader.ReadUInt16());
            }
        }

        return new MapAreaSection(flags, gridArea, areaFlags);
    }

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of ExtractedMapFileReader and keeps this workflow isolated from the caller.
      */
    private static MapHeightSection? ReadHeightSection(BinaryReader reader, MapFileHeader header)
    {
        if (header.HeightMapOffset == 0 || header.HeightMapSize == 0)
        {
            return null;
        }

        reader.BaseStream.Position = header.HeightMapOffset;
        string fourcc = ReadFourCC(reader);

        if (!string.Equals(fourcc, MapFormatConstants.HeightMagic, StringComparison.Ordinal))
        {
            throw new MapFormatException($"Invalid map height magic '{fourcc}'. Expected '{MapFormatConstants.HeightMagic}'.");
        }

        uint flags = reader.ReadUInt32();
        float gridHeight = reader.ReadSingle();
        float gridMaxHeight = reader.ReadSingle();

        int v9ValueCount = 0;
        int v8ValueCount = 0;

        if ((flags & MapFormatConstants.MapHeightNoHeight) == 0)
        {
            v9ValueCount = MapFormatConstants.V9VertexCount;
            v8ValueCount = MapFormatConstants.V8VertexCount;

            int bytesPerValue = GetHeightBytesPerValue(flags);
            long expectedSize = HeightHeaderSize + checked((long)(v9ValueCount + v8ValueCount) * bytesPerValue);

            if (expectedSize != header.HeightMapSize)
            {
                throw new MapFormatException($"Height section size mismatch. Header says {header.HeightMapSize} byte(s), calculated {expectedSize} byte(s).");
            }
        }

        return new MapHeightSection(flags, gridHeight, gridMaxHeight, v9ValueCount, v8ValueCount);
    }

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of ExtractedMapFileReader and keeps this workflow isolated from the caller.
      */
    private static MapLiquidSection? ReadLiquidSection(BinaryReader reader, MapFileHeader header)
    {
        if (header.LiquidMapOffset == 0 || header.LiquidMapSize == 0)
        {
            return null;
        }

        reader.BaseStream.Position = header.LiquidMapOffset;
        string fourcc = ReadFourCC(reader);

        if (!string.Equals(fourcc, MapFormatConstants.LiquidMagic, StringComparison.Ordinal))
        {
            throw new MapFormatException($"Invalid map liquid magic '{fourcc}'. Expected '{MapFormatConstants.LiquidMagic}'.");
        }

        ushort flags = reader.ReadUInt16();
        ushort liquidType = reader.ReadUInt16();
        byte offsetX = reader.ReadByte();
        byte offsetY = reader.ReadByte();
        byte width = reader.ReadByte();
        byte height = reader.ReadByte();
        float liquidLevel = reader.ReadSingle();

        if (header.LiquidMapSize < LiquidHeaderSize)
        {
            throw new MapFormatException($"Liquid section size {header.LiquidMapSize} is smaller than the liquid header size {LiquidHeaderSize}.");
        }

        return new MapLiquidSection(flags, liquidType, offsetX, offsetY, width, height, liquidLevel);
    }

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of ExtractedMapFileReader and keeps this workflow isolated from the caller.
      */
    private static void ValidateHeader(MapFileHeader header, long length, string path)
    {
        if (!string.Equals(header.MapMagic, MapFormatConstants.MapMagic, StringComparison.Ordinal))
        {
            throw new MapFormatException($"{path} has invalid map magic '{header.MapMagic}'. Expected '{MapFormatConstants.MapMagic}'.");
        }

        if (!string.Equals(header.VersionMagic, MapFormatConstants.VersionMagic, StringComparison.Ordinal))
        {
            throw new MapFormatException($"{path} has invalid map version '{header.VersionMagic}'. Expected '{MapFormatConstants.VersionMagic}'.");
        }

        ValidateRange(header.AreaMapOffset, header.AreaMapSize, length, "area");
        ValidateRange(header.HeightMapOffset, header.HeightMapSize, length, "height");
        ValidateRange(header.LiquidMapOffset, header.LiquidMapSize, length, "liquid");
        ValidateRange(header.HolesOffset, header.HolesSize, length, "holes");

        if (header.AreaMapSize != 0 && header.AreaMapOffset < MapFileHeaderSize)
        {
            throw new MapFormatException($"Area section starts at {header.AreaMapOffset}, but the map header is {MapFileHeaderSize} byte(s).");
        }

        if (header.AreaMapSize != 0 && header.AreaMapSize < AreaHeaderSize)
        {
            throw new MapFormatException($"Area section size {header.AreaMapSize} is smaller than the area header size {AreaHeaderSize}.");
        }

        if (header.HeightMapSize != 0 && header.HeightMapSize < HeightHeaderSize)
        {
            throw new MapFormatException($"Height section size {header.HeightMapSize} is smaller than the height header size {HeightHeaderSize}.");
        }
    }

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of ExtractedMapFileReader and keeps this workflow isolated from the caller.
      */
    private static void ValidateRange(uint offset, uint size, long fileLength, string sectionName)
    {
        if (offset == 0 && size == 0)
        {
            return;
        }

        long end = checked((long)offset + size);

        if (offset == 0 || size == 0 || end > fileLength)
        {
            throw new MapFormatException($"Invalid {sectionName} section range. Offset={offset}, Size={size}, FileLength={fileLength}.");
        }
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of ExtractedMapFileReader and keeps this workflow isolated from the caller.
      */
    private static int GetHeightBytesPerValue(uint flags)
    {
        if ((flags & MapFormatConstants.MapHeightAsInt8) != 0)
        {
            return sizeof(byte);
        }

        if ((flags & MapFormatConstants.MapHeightAsInt16) != 0)
        {
            return sizeof(ushort);
        }

        return sizeof(float);
    }

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of ExtractedMapFileReader and keeps this workflow isolated from the caller.
      */
    private static string ReadFourCC(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);

        if (bytes.Length != 4)
        {
            throw new EndOfStreamException("Unexpected end of stream while reading FourCC value.");
        }

        return Encoding.ASCII.GetString(bytes);
    }
}
