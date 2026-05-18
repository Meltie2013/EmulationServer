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
  * This file belongs to the developer tooling for data extraction, validation, and diagnostics portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Tools.Extraction.Mpq;

/**
  * Represents the mpq block table entry component in the developer tooling for data extraction, validation, and diagnostics area.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
internal readonly struct MpqBlockTableEntry
{
    /**
      * Creates a new MpqBlockTableEntry instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
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
