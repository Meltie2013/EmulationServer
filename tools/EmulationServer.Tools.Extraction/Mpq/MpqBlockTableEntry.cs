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
  * File overview: tools/EmulationServer.Tools.Extraction/Mpq/MpqBlockTableEntry.cs
  * Documents the MpqBlockTableEntry source file in the client data extraction and conversion tooling area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Tools.Extraction.Mpq;

/**
  * Owns the mpq block table entry behavior for the client data extraction and conversion tooling layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
internal readonly struct MpqBlockTableEntry
{
    /**
      * Initializes a new MpqBlockTableEntry instance with the dependencies required by the client data extraction and conversion tooling workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: filePosition, compressedSize, fileSize, flags.
      */
    public MpqBlockTableEntry(uint filePosition, uint compressedSize, uint fileSize, MpqFileFlags flags)
    {
        FilePosition = filePosition;
        CompressedSize = compressedSize;
        FileSize = fileSize;
        Flags = flags;
    }

    /**
      * Gets or stores the file position value used by MpqBlockTableEntry.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public uint FilePosition { get; }

    /**
      * Gets or stores the compressed size value used by MpqBlockTableEntry.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public uint CompressedSize { get; }

    /**
      * Gets or stores the file size value used by MpqBlockTableEntry.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public uint FileSize { get; }

    /**
      * Gets or stores the flags value used by MpqBlockTableEntry.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public MpqFileFlags Flags { get; }

    /**
      * Gets or stores the exists value used by MpqBlockTableEntry.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public bool Exists => Flags.HasFlag(MpqFileFlags.Exists) && !Flags.HasFlag(MpqFileFlags.DeleteMarker);

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of MpqBlockTableEntry and keeps this workflow isolated from the caller.
      */
    public static MpqBlockTableEntry Read(ReadOnlySpan<byte> data)
    {
        return new MpqBlockTableEntry(
            BinaryPrimitives.ReadUInt32LittleEndian(data[0..4]),
            BinaryPrimitives.ReadUInt32LittleEndian(data[4..8]),
            BinaryPrimitives.ReadUInt32LittleEndian(data[8..12]),
            (MpqFileFlags)BinaryPrimitives.ReadUInt32LittleEndian(data[12..16]));
    }
}
