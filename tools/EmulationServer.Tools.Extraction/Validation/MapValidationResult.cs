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
 * File overview: tools/EmulationServer.Tools.Extraction/Validation/MapValidationResult.cs
 * Documents the MapValidationResult source file in the client data extraction and conversion tooling area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Tools.Extraction.Validation;

/**
 * Owns the map validation result behavior for the client data extraction and conversion tooling layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public sealed class MapValidationResult
{
    /**
     * Holds the private messages state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly List<ValidationMessage> _messages = [];

    /**
      * Gets or stores the messages value used by MapValidationResult.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public IReadOnlyList<ValidationMessage> Messages => _messages;

    /**
      * Gets or stores the is valid value used by MapValidationResult.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public bool IsValid => _messages.All(message => message.Severity != ValidationSeverity.Error);

    /**
      * Adds a new item to the managed collection while preserving internal invariants.
      * The method is part of MapValidationResult and keeps this workflow isolated from the caller.
      */
    public void Add(ValidationSeverity severity, string message)
    {
        _messages.Add(new ValidationMessage(severity, message));
    }

    /**
      * Adds a new item to the managed collection while preserving internal invariants.
      * The method is part of MapValidationResult and keeps this workflow isolated from the caller.
      */
    public void AddInfo(string message)
    {
        Add(ValidationSeverity.Info, message);
    }

    /**
      * Adds a new item to the managed collection while preserving internal invariants.
      * The method is part of MapValidationResult and keeps this workflow isolated from the caller.
      */
    public void AddWarning(string message)
    {
        Add(ValidationSeverity.Warning, message);
    }

    /**
      * Adds a new item to the managed collection while preserving internal invariants.
      * The method is part of MapValidationResult and keeps this workflow isolated from the caller.
      */
    public void AddError(string message)
    {
        Add(ValidationSeverity.Error, message);
    }
}
