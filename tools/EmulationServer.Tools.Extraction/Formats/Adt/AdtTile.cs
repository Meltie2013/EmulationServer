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
  * File overview: tools/EmulationServer.Tools.Extraction/Formats/Adt/AdtTile.cs
  * Documents the AdtTile source file in the client data extraction and conversion tooling area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Tools.Extraction.Formats.Adt;

/**
  * Owns the adt tile behavior for the client data extraction and conversion tooling layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class AdtTile
{
    /**
      * Initializes a new AdtTile instance with the dependencies required by the client data extraction and conversion tooling workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: path, cells, liquid.
      */
    public AdtTile(string path, IReadOnlyList<AdtCell> cells, AdtLiquidData liquid)
    {
        Path = path;
        Cells = cells;
        Liquid = liquid;
    }

    /**
      * Gets or stores the path value used by AdtTile.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string Path { get; }

    /**
      * Gets or stores the cells value used by AdtTile.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public IReadOnlyList<AdtCell> Cells { get; }

    /**
      * Gets or stores the liquid value used by AdtTile.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public AdtLiquidData Liquid { get; }
}
