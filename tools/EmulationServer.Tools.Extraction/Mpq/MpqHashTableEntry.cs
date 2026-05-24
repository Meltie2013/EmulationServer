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

/**
  * File overview: tools/EmulationServer.Tools.Extraction/Mpq/MpqHashTableEntry.cs
  * Documents the MpqHashTableEntry source file in the client data extraction and conversion tooling area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Tools.Extraction.Mpq;

/**
  * Owns the mpq hash table entry behavior for the client data extraction and conversion tooling layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
internal readonly struct MpqHashTableEntry
{
    /**
      * Initializes a new MpqHashTableEntry instance with the dependencies required by the client data extraction and conversion tooling workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: nameHashA, nameHashB, locale, platform, blockIndex.
      */
    public MpqHashTableEntry(uint nameHashA, uint nameHashB, ushort locale, ushort platform, uint blockIndex)
    {
        NameHashA = nameHashA;
        NameHashB = nameHashB;
        Locale = locale;
        Platform = platform;
        BlockIndex = blockIndex;
    }

    /**
      * Gets or stores the name hash a value used by MpqHashTableEntry.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public uint NameHashA { get; }

    /**
      * Gets or stores the name hash b value used by MpqHashTableEntry.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public uint NameHashB { get; }

    /**
      * Gets or stores the locale value used by MpqHashTableEntry.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public ushort Locale { get; }

    /**
      * Gets or stores the platform value used by MpqHashTableEntry.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public ushort Platform { get; }

    /**
      * Gets or stores the block index value used by MpqHashTableEntry.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public uint BlockIndex { get; }

    /**
      * Gets or stores the is empty value used by MpqHashTableEntry.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public bool IsEmpty => BlockIndex == 0xFFFFFFFF;

    /**
      * Gets or stores the is deleted value used by MpqHashTableEntry.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public bool IsDeleted => BlockIndex == 0xFFFFFFFE;

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of MpqHashTableEntry and keeps this workflow isolated from the caller.
      */
    public static MpqHashTableEntry Read(ReadOnlySpan<byte> data)
    {
        return new MpqHashTableEntry(
            BinaryPrimitives.ReadUInt32LittleEndian(data[0..4]),
            BinaryPrimitives.ReadUInt32LittleEndian(data[4..8]),
            BinaryPrimitives.ReadUInt16LittleEndian(data[8..10]),
            BinaryPrimitives.ReadUInt16LittleEndian(data[10..12]),
            BinaryPrimitives.ReadUInt32LittleEndian(data[12..16]));
    }
}
