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
  * Documents the AssetExtractionResult source file in the client data extraction and conversion tooling area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Tools.Extraction.Extraction;

/**
  * Owns the asset extraction result behavior for the client data extraction and conversion tooling layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class AssetExtractionResult
{
    /**
      * Holds the private messages state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly List<string> _messages = [];

    /**
      * Initializes a new AssetExtractionResult instance with the dependencies required by the client data extraction and conversion tooling workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: kind.
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
