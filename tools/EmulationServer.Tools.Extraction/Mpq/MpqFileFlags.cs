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

namespace EmulationServer.Tools.Extraction.Mpq;

[Flags]
internal enum MpqFileFlags : uint
{
    Imploded = 0x00000100,
    Compressed = 0x00000200,
    Encrypted = 0x00010000,
    FixKey = 0x00020000,
    SingleUnit = 0x01000000,
    DeleteMarker = 0x02000000,
    SectorCrc = 0x04000000,
    Exists = 0x80000000,
}
