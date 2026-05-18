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

/**
  * File overview: tools/EmulationServer.Tools.Extraction/Mpq/MpqFileFlags.cs
  * This file belongs to the developer tooling for data extraction, validation, and diagnostics portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Tools.Extraction.Mpq;

[Flags]
/**
  * Defines the allowed mpq file flags values used to keep state and protocol decisions explicit.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
internal enum MpqFileFlags : uint
{
    /**
      * Represents the imploded value for MpqFileFlags.
      */
    Imploded = 0x00000100,
    /**
      * Represents the compressed value for MpqFileFlags.
      */
    Compressed = 0x00000200,
    /**
      * Represents the encrypted value for MpqFileFlags.
      */
    Encrypted = 0x00010000,
    /**
      * Represents the fix key value for MpqFileFlags.
      */
    FixKey = 0x00020000,
    /**
      * Represents the single unit value for MpqFileFlags.
      */
    SingleUnit = 0x01000000,
    /**
      * Represents the delete marker value for MpqFileFlags.
      */
    DeleteMarker = 0x02000000,
    /**
      * Represents the sector crc value for MpqFileFlags.
      */
    SectorCrc = 0x04000000,
    /**
      * Represents the exists value for MpqFileFlags.
      */
    Exists = 0x80000000,
}
