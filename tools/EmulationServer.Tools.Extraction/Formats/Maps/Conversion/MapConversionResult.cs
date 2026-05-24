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
 * File overview: tools/EmulationServer.Tools.Extraction/Formats/Maps/Conversion/MapConversionResult.cs
 * Documents the MapConversionResult source file in the client data extraction and conversion tooling area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Tools.Extraction.Formats.Maps.Conversion;

/**
 * Owns the map conversion result behavior for the client data extraction and conversion tooling layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public sealed class MapConversionResult
{
    /**
      * Gets or stores the source files value used by MapConversionResult.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int SourceFiles { get; set; }

    /**
      * Gets or stores the converted files value used by MapConversionResult.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int ConvertedFiles { get; set; }

    /**
      * Gets or stores the skipped files value used by MapConversionResult.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int SkippedFiles { get; set; }

    /**
      * Gets or stores the failed files value used by MapConversionResult.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int FailedFiles { get; set; }

    /**
      * Gets or stores the messages value used by MapConversionResult.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public List<string> Messages { get; } = [];
}
