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
// File: src/EmulationServer.Shared/Logging/Enum/LogType.cs
// Purpose: Contains log type code for the shared infrastructure, logging, timing, and cross-service utility layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Shared.Logging.Enums;

// Type: LogType
// Purpose: Defines the allowed log type values used by the shared infrastructure, logging, timing, and cross-service utility layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public enum LogType
{

    // Enum Value: Defines the NETWORK enum value.
    // Value: next sequential value assigned by C#.
    NETWORK,

    // Enum Value: Defines the DEBUG enum value.
    // Value: next sequential value assigned by C#.
    DEBUG,

    // Enum Value: Defines the INFORMATION enum value.
    // Value: next sequential value assigned by C#.
    INFORMATION,

    // Enum Value: Defines the SYSTEM enum value.
    // Value: next sequential value assigned by C#.
    SYSTEM,

    // Enum Value: Defines the SUCCESS enum value.
    // Value: next sequential value assigned by C#.
    SUCCESS,

    // Enum Value: Defines the WARNING enum value.
    // Value: next sequential value assigned by C#.
    WARNING,

    // Enum Value: Defines the FAILED enum value.
    // Value: next sequential value assigned by C#.
    FAILED,

    // Enum Value: Defines the CRITICAL enum value.
    // Value: next sequential value assigned by C#.
    CRITICAL,

    // Enum Value: Defines the DATABASE enum value.
    // Value: next sequential value assigned by C#.
    DATABASE,

    // Enum Value: Defines the ALERT enum value.
    // Value: next sequential value assigned by C#.
    ALERT,

    // Enum Value: Defines the EMERG enum value.
    // Value: next sequential value assigned by C#.
    EMERG,

    // Enum Value: Defines the FUNC enum value.
    // Value: next sequential value assigned by C#.
    FUNC,

    // Enum Value: Defines the NOTICE enum value.
    // Value: next sequential value assigned by C#.
    NOTICE,

    // Enum Value: Defines the THREAD enum value.
    // Value: next sequential value assigned by C#.
    THREAD,
    // Enum Value: Defines the TRACE enum value.
    // Value: next sequential value assigned by C#.
    TRACE
}
