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
// File: src/EmulationServer.Shared/Logging/Configuration/LogOutputMode.cs
// Purpose: Contains log output mode code for the shared infrastructure, logging, timing, and cross-service utility layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Shared.Logging.Configuration;

// Type: LogOutputMode
// Purpose: Defines the allowed log output mode values used by the shared infrastructure, logging, timing, and cross-service utility layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public enum LogOutputMode
{

    // Enum Value: Defines the console enum value.
    // Value: next sequential value assigned by C#.
    Console,

    // Enum Value: Defines the file enum value.
    // Value: next sequential value assigned by C#.
    File,

    // Enum Value: Defines the both enum value.
    // Value: next sequential value assigned by C#.
    Both
}
