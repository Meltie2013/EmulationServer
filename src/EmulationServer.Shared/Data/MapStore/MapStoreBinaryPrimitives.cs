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

namespace EmulationServer.Shared.Data.MapStore;

/**
  * Centralizes small binary read/write helpers used by mapstore writers, validators, and runtime readers.
  */
public static class MapStoreBinaryPrimitives
{
    /**
      * Reads a fixed-size ASCII value.
      */
    public static string ReadAscii(BinaryReader reader, int byteCount, string valueName)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentException.ThrowIfNullOrWhiteSpace(valueName);

        if (byteCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(byteCount), byteCount, "ASCII byte count must be greater than zero.");
        }

        byte[] bytes = reader.ReadBytes(byteCount);
        if (bytes.Length != byteCount)
        {
            throw new EndOfStreamException($"Unexpected end of stream while reading {valueName}.");
        }

        return Encoding.ASCII.GetString(bytes);
    }

    /**
      * Reads one four-character ASCII value.
      */
    public static string ReadFourCC(BinaryReader reader, string valueName = "FourCC value")
    {
        return ReadAscii(reader, 4, valueName);
    }

    /**
      * Writes a fixed-size ASCII value.
      */
    public static void WriteAscii(BinaryWriter writer, string value)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        writer.Write(Encoding.ASCII.GetBytes(value));
    }

    /**
      * Writes one four-character ASCII value.
      */
    public static void WriteFourCC(BinaryWriter writer, string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        byte[] bytes = Encoding.ASCII.GetBytes(value);
        if (bytes.Length != 4)
        {
            throw new ArgumentException("FourCC values must be exactly four bytes.", nameof(value));
        }

        writer.Write(bytes);
    }

    /**
      * Reads a length-prefixed UTF-8 string.
      */
    public static string ReadUtf8String(BinaryReader reader, string path, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        int length = reader.ReadInt32();
        long remaining = reader.BaseStream.Length - reader.BaseStream.Position;
        if (length < 0 || length > remaining)
        {
            throw new InvalidDataException($"{path} has invalid {fieldName} string length {length}.");
        }

        byte[] bytes = reader.ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }

    /**
      * Writes a length-prefixed UTF-8 string.
      */
    public static void WriteUtf8String(BinaryWriter writer, string value)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        byte[] bytes = Encoding.UTF8.GetBytes(value);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }
}
