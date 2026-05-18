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
  * File overview: tools/EmulationServer.Tools.Extraction/Extraction/AssetExtractionKind.cs
  * This file belongs to the developer tooling for data extraction, validation, and diagnostics portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Tools.Extraction.Extraction;

/**
  * Defines the allowed asset extraction kind values used to keep state and protocol decisions explicit.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public enum AssetExtractionKind
{
    /**
      * Represents the dbc value for AssetExtractionKind.
      */
    Dbc = 0,
    /**
      * Represents the maps value for AssetExtractionKind.
      */
    Maps = 1,
    /**
      * Represents the vmaps value for AssetExtractionKind.
      */
    Vmaps = 2,
    /**
      * Represents the mmaps value for AssetExtractionKind.
      */
    Mmaps = 3,
}
