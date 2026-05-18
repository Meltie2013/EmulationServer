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

using System.Globalization;
using System.Text;

namespace EmulationServer.Game.Data.Maps;

public sealed class MapTileDataStore
{
    private const int MapFileHeaderSize = 44;
    private const string ExpectedMapMagic = "MAPS";
    private const string ExpectedVersionMagic = "0000";

    private MapTileDataStore(string path, MapTileKey key, MapFileHeader header, byte[] data)
    {
        Path = path;
        Name = System.IO.Path.GetFileName(path);
        Key = key;
        Header = header;
        Data = data;
    }

    public string Path { get; }

    public string Name { get; }

    public MapTileKey Key { get; }

    public MapFileHeader Header { get; }

    public byte[] Data { get; }

    public static MapTileDataStore Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        byte[] data = File.ReadAllBytes(path);
        using MemoryStream stream = new(data, writable: false);
        using BinaryReader reader = new(stream, Encoding.ASCII, leaveOpen: false);

        MapFileHeader header = ReadHeader(reader, path);
        ValidateHeader(header, data.LongLength, path);
        MapTileKey key = ParseTileKey(path);

        return new MapTileDataStore(path, key, header, data);
    }

    private static MapFileHeader ReadHeader(BinaryReader reader, string path)
    {
        if (reader.BaseStream.Length < MapFileHeaderSize)
        {
            throw new MapFormatException($"{path} is too small to contain a map header.");
        }

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

    private static void ValidateHeader(MapFileHeader header, long length, string path)
    {
        if (!string.Equals(header.MapMagic, ExpectedMapMagic, StringComparison.Ordinal))
        {
            throw new MapFormatException($"{path} has invalid map magic '{header.MapMagic}'. Expected '{ExpectedMapMagic}'.");
        }

        if (!string.Equals(header.VersionMagic, ExpectedVersionMagic, StringComparison.Ordinal))
        {
            throw new MapFormatException($"{path} has invalid map version '{header.VersionMagic}'. Expected '{ExpectedVersionMagic}'.");
        }

        ValidateRange(header.AreaMapOffset, header.AreaMapSize, length, path, "area");
        ValidateRange(header.HeightMapOffset, header.HeightMapSize, length, path, "height");
        ValidateRange(header.LiquidMapOffset, header.LiquidMapSize, length, path, "liquid");
        ValidateRange(header.HolesOffset, header.HolesSize, length, path, "holes");
    }

    private static void ValidateRange(uint offset, uint size, long fileLength, string path, string sectionName)
    {
        if (offset == 0 && size == 0)
        {
            return;
        }

        long end = checked((long)offset + size);
        if (offset == 0 || size == 0 || end > fileLength)
        {
            throw new MapFormatException($"{path} has an invalid {sectionName} section range. Offset={offset}, Size={size}, FileLength={fileLength}.");
        }
    }

    private static MapTileKey ParseTileKey(string path)
    {
        string name = System.IO.Path.GetFileNameWithoutExtension(path);

        if (name.Length != 7 || !uint.TryParse(name.AsSpan(0, 3), NumberStyles.None, CultureInfo.InvariantCulture, out uint mapId) ||
            !byte.TryParse(name.AsSpan(3, 2), NumberStyles.None, CultureInfo.InvariantCulture, out byte tileX) ||
            !byte.TryParse(name.AsSpan(5, 2), NumberStyles.None, CultureInfo.InvariantCulture, out byte tileY))
        {
            throw new MapFormatException($"Map file '{path}' must use MaNGOS tile file naming: <mapId:000><tileX:00><tileY:00>.map.");
        }

        return new MapTileKey(mapId, tileX, tileY);
    }

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
