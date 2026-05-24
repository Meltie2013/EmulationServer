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
  * File overview: src/EmulationServer.Game/Players/MapAvailabilityResult.cs
  * Documents the MapAvailabilityResult source file in the logged-in player state, persistence models, and gameplay records area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Players;

/**
  * Carries immutable map availability result data for the logged-in player state, persistence models, and gameplay records layer.
  * Records in this project are used as explicit transfer models so packet parsing, database repositories, and runtime systems can pass strongly typed values without mutating shared state.
  * Positional fields carried by this record: IsAvailable, Reason, OwnerServerName, RequiresInstanceServer.
  */
public sealed record MapAvailabilityResult(bool IsAvailable, string Reason, string OwnerServerName, bool RequiresInstanceServer)
{
    /**
      * Performs the available operation for the logged-in player state, persistence models, and gameplay records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: ownerServerName, requiresInstanceServer.
      */
    public static MapAvailabilityResult Available(string ownerServerName, bool requiresInstanceServer = false)
    {
        return new MapAvailabilityResult(true, string.Empty, ownerServerName, requiresInstanceServer);
    }

    /**
      * Performs the unavailable operation for the logged-in player state, persistence models, and gameplay records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: reason, requiresInstanceServer.
      */
    public static MapAvailabilityResult Unavailable(string reason, bool requiresInstanceServer = false)
    {
        return new MapAvailabilityResult(false, reason, string.Empty, requiresInstanceServer);
    }
}
