using System.Text;
using EmulationServer.Tools.Extraction.Formats.Maps;
using EmulationServer.Tools.Extraction.Validation;

namespace EmulationServer.Tests.Tools;

public sealed class ExtractedMapFileReaderTests
{
    [Fact]
    public void Read_WithLiquidSection_ReturnsLiquidMetadata()
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.map");

        try
        {
            WriteMinimalMapWithLiquid(path);

            ExtractedMapFile map = ExtractedMapFileReader.Read(path);

            Assert.NotNull(map.Liquid);
            Assert.Equal((ushort)(MapFormatConstants.MapLiquidNoType | MapFormatConstants.MapLiquidNoHeight), map.Liquid.Flags);
            Assert.Equal((ushort)MapFormatConstants.MapLiquidTypeWater, map.Liquid.LiquidType);
            Assert.Equal(4, map.Liquid.OffsetX);
            Assert.Equal(5, map.Liquid.OffsetY);
            Assert.Equal(10, map.Liquid.Width);
            Assert.Equal(11, map.Liquid.Height);
            Assert.Equal(123.25f, map.Liquid.LiquidLevel);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Verify_WithLiquidSection_DoesNotReportLiquidErrors()
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.map");

        try
        {
            WriteMinimalMapWithLiquid(path);

            ExtractedMapFile map = ExtractedMapFileReader.Read(path);
            MapValidationResult result = new MapDataVerifier().Verify(map);

            Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Messages.Select(message => message.Message)));
            Assert.Contains(result.Messages, message => message.Message.Contains("liquid type=", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void WriteMinimalMapWithLiquid(string path)
    {
        byte[] areaSection = BuildAreaSection();
        byte[] heightSection = BuildHeightSection();
        byte[] liquidSection = BuildLiquidSection();

        uint areaOffset = MapFormatConstants.MapFileHeaderSize;
        uint areaSize = checked((uint)areaSection.Length);
        uint heightOffset = checked(areaOffset + areaSize);
        uint heightSize = checked((uint)heightSection.Length);
        uint liquidOffset = checked(heightOffset + heightSize);
        uint liquidSize = checked((uint)liquidSection.Length);

        using FileStream stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: false);

        WriteFourCC(writer, MapFormatConstants.MapMagic);
        WriteFourCC(writer, MapFormatConstants.VersionMagic);
        writer.Write((uint)5875);
        writer.Write(areaOffset);
        writer.Write(areaSize);
        writer.Write(heightOffset);
        writer.Write(heightSize);
        writer.Write(liquidOffset);
        writer.Write(liquidSize);
        writer.Write((uint)0);
        writer.Write((uint)0);

        writer.Write(areaSection);
        writer.Write(heightSection);
        writer.Write(liquidSection);
    }

    private static byte[] BuildAreaSection()
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true);

        WriteFourCC(writer, MapFormatConstants.AreaMagic);
        writer.Write(MapFormatConstants.MapAreaNoArea);
        writer.Write((ushort)1);

        writer.Flush();
        return stream.ToArray();
    }

    private static byte[] BuildHeightSection()
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true);

        WriteFourCC(writer, MapFormatConstants.HeightMagic);
        writer.Write(MapFormatConstants.MapHeightNoHeight);
        writer.Write(0.0f);
        writer.Write(0.0f);

        writer.Flush();
        return stream.ToArray();
    }

    private static byte[] BuildLiquidSection()
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true);

        WriteFourCC(writer, MapFormatConstants.LiquidMagic);
        writer.Write((ushort)(MapFormatConstants.MapLiquidNoType | MapFormatConstants.MapLiquidNoHeight));
        writer.Write((ushort)MapFormatConstants.MapLiquidTypeWater);
        writer.Write((byte)4);
        writer.Write((byte)5);
        writer.Write((byte)10);
        writer.Write((byte)11);
        writer.Write(123.25f);

        writer.Flush();
        return stream.ToArray();
    }

    private static void WriteFourCC(BinaryWriter writer, string fourCC)
    {
        writer.Write(Encoding.ASCII.GetBytes(fourCC));
    }
}
