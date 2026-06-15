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

namespace EmulationServer.Game.WorldData;

/**
  * Carries immutable gameobject_template data from the world database.
  * The data fields intentionally mirror the Mangos Zero layout so object-specific behavior can be added per type without changing the row loader later.
  */
public sealed record GameObjectTemplateRecord(
    uint Entry,
    byte Type,
    uint DisplayId,
    string Name,
    ushort Faction,
    uint Flags,
    float Size,
    IReadOnlyList<uint> DataFields,
    uint MinGold,
    uint MaxGold,
    string ScriptName)
{
    /**
      * Mangos Zero stores 24 type-specific data columns on each game object template.
      */
    public const int DataFieldCount = 24;

    /**
      * Resolves one type-specific data field without exposing callers to list bounds checks.
      */
    public uint GetDataField(int index)
    {
        return index >= 0 && index < DataFields.Count ? DataFields[index] : 0u;
    }
}
