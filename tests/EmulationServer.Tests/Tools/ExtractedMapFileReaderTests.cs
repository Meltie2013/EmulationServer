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
// File: tests/EmulationServer.Tests/Tools/ExtractedMapFileReaderTests.cs
// Purpose: Contains extracted map file reader tests code for the automated test and verification layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Text;
using EmulationServer.Tools.Extraction.Formats.Maps;
using EmulationServer.Tools.Extraction.Validation;

namespace EmulationServer.Tests.Tools;

// Type: ExtractedMapFileReaderTests
// Purpose: Provides extracted map file reader tests behavior for the automated test and verification layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class ExtractedMapFileReaderTests
{

    [Fact]
    // Method: Read_WithLiquidSection_ReturnsLiquidMetadata
    // Purpose: Retrieves read with liquid section returns liquid metadata data for the automated test and verification layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to ExtractedMapFileReaderTests so callers do not duplicate validation, protocol, or persistence rules.
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
    // Method: Verify_WithLiquidSection_DoesNotReportLiquidErrors
    // Purpose: Executes the verify with liquid section does not report liquid errors operation for the automated test and verification layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to ExtractedMapFileReaderTests so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: WriteMinimalMapWithLiquid
    // Purpose: Builds or writes write minimal map with liquid output for the automated test and verification layer.
    // Parameters:
    // - path: Path value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ExtractedMapFileReaderTests so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: BuildAreaSection
    // Purpose: Builds or writes build area section output for the automated test and verification layer.
    // Parameters: none.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to ExtractedMapFileReaderTests so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: BuildHeightSection
    // Purpose: Builds or writes build height section output for the automated test and verification layer.
    // Parameters: none.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to ExtractedMapFileReaderTests so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: BuildLiquidSection
    // Purpose: Builds or writes build liquid section output for the automated test and verification layer.
    // Parameters: none.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to ExtractedMapFileReaderTests so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: WriteFourCC
    // Purpose: Builds or writes write four CC output for the automated test and verification layer.
    // Parameters:
    // - writer: Writer value supplied by the caller for this operation.
    // - fourCC: Four CC value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ExtractedMapFileReaderTests so callers do not duplicate validation, protocol, or persistence rules.
    private static void WriteFourCC(BinaryWriter writer, string fourCC)
    {
        writer.Write(Encoding.ASCII.GetBytes(fourCC));
    }
}
