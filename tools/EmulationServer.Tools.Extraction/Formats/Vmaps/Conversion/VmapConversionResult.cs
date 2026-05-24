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
  * File overview: tools/EmulationServer.Tools.Extraction/Formats/Vmaps/Conversion/VmapConversionResult.cs
  * Documents the VmapConversionResult source file in the client data extraction and conversion tooling area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Tools.Extraction.Formats.Vmaps.Conversion;

/**
  * Owns the vmap conversion result behavior for the client data extraction and conversion tooling layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class VmapConversionResult
{
    /**
      * Gets or stores how many WMO root source files were inspected.
      */
    public int SourceModelFiles { get; set; }

    /**
      * Gets or stores how many compact model files were written.
      */
    public int ConvertedModelFiles { get; set; }

    /**
      * Gets or stores how many model files were skipped because they were not needed or already existed.
      */
    public int SkippedModelFiles { get; set; }

    /**
      * Gets or stores how many model conversion failures occurred.
      */
    public int FailedModelFiles { get; set; }

    /**
      * Gets or stores how many ADT files were inspected for WMO placement data.
      */
    public int SourcePlacementFiles { get; set; }

    /**
      * Gets or stores how many placement tile files were written.
      */
    public int ConvertedPlacementFiles { get; set; }

    /**
      * Gets or stores how many placement tile files were skipped.
      */
    public int SkippedPlacementFiles { get; set; }

    /**
      * Gets or stores how many placement conversion failures occurred.
      */
    public int FailedPlacementFiles { get; set; }

    /**
      * Gets the conversion messages that should be shown to the user.
      */
    public List<string> Messages { get; } = [];
}
