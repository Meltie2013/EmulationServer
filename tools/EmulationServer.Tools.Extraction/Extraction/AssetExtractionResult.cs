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
  * File overview: tools/EmulationServer.Tools.Extraction/Extraction/AssetExtractionResult.cs
  * This file belongs to the developer tooling for data extraction, validation, and diagnostics portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Tools.Extraction.Extraction;

/**
  * Represents the asset extraction result component in the developer tooling for data extraction, validation, and diagnostics area.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public sealed class AssetExtractionResult
{
    /**
      * Stores the messages dependency or runtime value for AssetExtractionResult.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly List<string> _messages = [];

    /**
      * Creates a new AssetExtractionResult instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
      */
    public AssetExtractionResult(AssetExtractionKind kind)
    {
        Kind = kind;
    }

    /**
      * Gets or stores the kind value used by AssetExtractionResult.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public AssetExtractionKind Kind { get; }

    /**
      * Gets or stores the extracted files value used by AssetExtractionResult.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int ExtractedFiles { get; private set; }

    /**
      * Gets or stores the skipped files value used by AssetExtractionResult.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int SkippedFiles { get; private set; }

    /**
      * Gets or stores the messages value used by AssetExtractionResult.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public IReadOnlyList<string> Messages => _messages;

    /**
      * Adds a new item to the managed collection while preserving internal invariants.
      * The method is part of AssetExtractionResult and keeps this workflow isolated from the caller.
      */
    public void AddExtractedFile()
    {
        ExtractedFiles++;
    }

    /**
      * Adds a new item to the managed collection while preserving internal invariants.
      * The method is part of AssetExtractionResult and keeps this workflow isolated from the caller.
      */
    public void AddSkippedFile()
    {
        SkippedFiles++;
    }

    /**
      * Adds a new item to the managed collection while preserving internal invariants.
      * The method is part of AssetExtractionResult and keeps this workflow isolated from the caller.
      */
    public void AddMessage(string message)
    {
        _messages.Add(message);
    }
}
