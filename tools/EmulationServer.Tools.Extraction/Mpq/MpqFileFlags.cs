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
 * Documents the MpqFileFlags source file in the client data extraction and conversion tooling area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Tools.Extraction.Mpq;

/**
 * Lists the supported mpq file flags values used by the client data extraction and conversion tooling layer.
 * Numeric values are part of the project contract and should only be changed when the related client packet, DBC value, or database schema is updated as well.
 */
[Flags]
internal enum MpqFileFlags : uint
{
    /**
     * Represents the imploded value for mpq file flags handling.
     */
    Imploded = 0x00000100,
    /**
     * Represents the compressed value for mpq file flags handling.
     */
    Compressed = 0x00000200,
    /**
     * Represents the encrypted value for mpq file flags handling.
     */
    Encrypted = 0x00010000,
    /**
     * Represents the fix key value for mpq file flags handling.
     */
    FixKey = 0x00020000,
    /**
     * Represents the single unit value for mpq file flags handling.
     */
    SingleUnit = 0x01000000,
    /**
     * Represents the delete marker value for mpq file flags handling.
     */
    DeleteMarker = 0x02000000,
    /**
     * Represents the sector crc value for mpq file flags handling.
     */
    SectorCrc = 0x04000000,
    /**
     * Represents the exists value for mpq file flags handling.
     */
    Exists = 0x80000000,
}
