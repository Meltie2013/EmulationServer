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
  * File overview: src/EmulationServer.Game/Players/CharacterGuid.cs
  * Documents the CharacterGuid source file in the logged-in player state, persistence models, and gameplay records area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Players;

/**
  * Owns the character guid behavior for the logged-in player state, persistence models, and gameplay records layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public static class CharacterGuid
{
    /**
      * MaNGOS Zero uses a zero high GUID for players and a 0x4000 high GUID for item/container objects.
      * Keeping these helpers centralized prevents low-guid collisions between character rows and item_instance rows.
      */
    private const ushort HighGuidItem = 0x4000;

    /**
      * Performs the to client guid operation for the logged-in player state, persistence models, and gameplay records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: lowGuid.
      */
    public static ulong ToClientGuid(uint lowGuid)
    {
        return ToPlayerGuid(lowGuid);
    }

    /**
      * Builds a Vanilla player ObjectGuid from the character low guid.
      */
    public static ulong ToPlayerGuid(uint lowGuid)
    {
        return lowGuid;
    }

    /**
      * Builds a Vanilla item/container ObjectGuid from the item_instance low guid.
      */
    public static ulong ToItemGuid(uint lowGuid)
    {
        return lowGuid == 0 ? 0 : ((ulong)HighGuidItem << 48) | lowGuid;
    }

    /**
      * Performs the from client guid operation for the logged-in player state, persistence models, and gameplay records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: clientGuid.
      */
    public static uint FromClientGuid(ulong clientGuid)
    {
        return (uint)(clientGuid & uint.MaxValue);
    }
}
