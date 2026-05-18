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
  * File overview: src/EmulationServer.Shared/Logging/Enum/LogType.cs
  * This file belongs to the logging configuration, formatting, filtering, and output routing portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Shared.Logging.Enums;

/**
  * Defines the allowed log type values used to keep state and protocol decisions explicit.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public enum LogType
{
    NETWORK,       // Network code debugging
    DEBUG,         // Packet processing
    INFORMATION,   // General information
    USER,          // User actions
    SUCCESS,       // Successful operations
    WARNING,       // Warning conditions
    FAILED,        // Processing errors
    CRITICAL,      // Application errors
    DATABASE,      // Database operations/errors
    ALERT,         // Immediate action required
    EMERG,         // System unusable
    FUNC,          // Function tracing
    NOTICE,        // Significant condition
    THREAD,        // Thread tracing
    TRACE          // Fine-grained debugging
}
