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

namespace EmulationServer.WorldServer.Networking.Packets;

public enum CharacterCreateResult : byte
{
    Success = 0x2E,
    Error = 0x2F,
    Failed = 0x30,
    NameInUse = 0x31,
    Disabled = 0x32,
    PvPTeamsViolation = 0x33,
    ServerLimit = 0x34,
    AccountLimit = 0x35,
    ServerQueue = 0x36,
    OnlyExisting = 0x37,
    Expansion = 0x38,
    NameInvalid = 0x39,
    NameProfane = 0x3A,
    NameReserved = 0x3B,
}
