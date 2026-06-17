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
// File: src/EmulationServer.Shared/Logging/Models/LogMessage.cs
// Purpose: Contains log message code for the shared infrastructure, logging, timing, and cross-service utility layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Shared.Logging.Models;

// Type: LogMessage
// Purpose: Provides log message behavior for the shared infrastructure, logging, timing, and cross-service utility layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class LogMessage
{

    // Property: Gets or sets the timestamp value used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: timestamp value exposed by the owning type.
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    // Property: Gets or sets the type value used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: type value exposed by the owning type.
    public LogType Type { get; init; }

    // Property: Gets or sets the message value used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: message value exposed by the owning type.
    public string Message { get; init; } = string.Empty;

    // Property: Gets or sets the category value used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: category value exposed by the owning type.
    public string? Category { get; init; }
}
