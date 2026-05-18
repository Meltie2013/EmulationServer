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

namespace EmulationServer.Tools.Extraction.Formats.Maps;

public sealed record MapHeightSection(
    uint Flags,
    float GridHeight,
    float GridMaxHeight,
    int V9ValueCount,
    int V8ValueCount)
{
    public bool HasHeight => (Flags & MapFormatConstants.MapHeightNoHeight) == 0;

    public bool IsInt8Encoded => (Flags & MapFormatConstants.MapHeightAsInt8) != 0;

    public bool IsInt16Encoded => (Flags & MapFormatConstants.MapHeightAsInt16) != 0;

    public bool IsFloatEncoded => HasHeight && !IsInt8Encoded && !IsInt16Encoded;

    public float HeightRange => GridMaxHeight - GridHeight;
}
