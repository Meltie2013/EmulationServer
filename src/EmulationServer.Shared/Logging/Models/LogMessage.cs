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

using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/EmulationServer.Shared/Logging/Models/LogMessage.cs
  * Documents the LogMessage source file in the shared configuration, logging, and utility support area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Shared.Logging.Models;

/**
  * Owns the log message behavior for the shared configuration, logging, and utility support layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class LogMessage
{
    /**
      * Gets or stores the timestamp value used by LogMessage.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /**
      * Gets or stores the type value used by LogMessage.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public LogType Type { get; init; }

    /**
      * Gets or stores the message value used by LogMessage.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string Message { get; init; } = string.Empty;

    /**
      * Gets or stores the category value used by LogMessage.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string? Category { get; init; }
}
