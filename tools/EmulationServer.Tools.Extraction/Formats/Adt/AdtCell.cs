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
  * File overview: tools/EmulationServer.Tools.Extraction/Formats/Adt/AdtCell.cs
  * Documents the AdtCell source file in the client data extraction and conversion tooling area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Tools.Extraction.Formats.Adt;

/**
  * Owns the adt cell behavior for the client data extraction and conversion tooling layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class AdtCell
{
    /**
      * Gets or stores the index x value used by AdtCell.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int IndexX { get; init; }

    /**
      * Gets or stores the index y value used by AdtCell.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int IndexY { get; init; }

    /**
      * Gets or stores the flags value used by AdtCell.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public uint Flags { get; init; }

    /**
      * Gets or stores the area id value used by AdtCell.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public uint AreaId { get; init; }

    /**
      * Gets or stores the holes value used by AdtCell.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public ushort Holes { get; init; }

    /**
      * Gets or stores the base height value used by AdtCell.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public float BaseHeight { get; init; }

    /**
      * Gets or stores the heights value used by AdtCell.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public float[] Heights { get; init; } = [];

    /**
      * Gets or stores the has heights value used by AdtCell.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public bool HasHeights => Heights.Length == 145;
}
