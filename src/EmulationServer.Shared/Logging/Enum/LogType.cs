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
 * Documents the LogType source file in the shared configuration, logging, and utility support area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Shared.Logging.Enums;

/**
 * Lists the supported log type values used by the shared configuration, logging, and utility support layer.
 * Numeric values are part of the project contract and should only be changed when the related client packet, DBC value, or database schema is updated as well.
 */
public enum LogType
{
    /**
     * Represents the network value for log type handling.
     */
    NETWORK,       // Network code debugging
    /**
     * Represents the debug value for log type handling.
     */
    DEBUG,         // Packet processing
    /**
     * Represents the information value for log type handling.
     */
    INFORMATION,   // General information
    /**
     * Represents the user value for log type handling.
     */
    USER,          // User actions
    /**
     * Represents the success value for log type handling.
     */
    SUCCESS,       // Successful operations
    /**
     * Represents the warning value for log type handling.
     */
    WARNING,       // Warning conditions
    /**
     * Represents the failed value for log type handling.
     */
    FAILED,        // Processing errors
    /**
     * Represents the critical value for log type handling.
     */
    CRITICAL,      // Application errors
    /**
     * Represents the database value for log type handling.
     */
    DATABASE,      // Database operations/errors
    /**
     * Represents the alert value for log type handling.
     */
    ALERT,         // Immediate action required
    /**
     * Represents the emerg value for log type handling.
     */
    EMERG,         // System unusable
    /**
     * Represents the func value for log type handling.
     */
    FUNC,          // Function tracing
    /**
     * Represents the notice value for log type handling.
     */
    NOTICE,        // Significant condition
    /**
     * Represents the thread value for log type handling.
     */
    THREAD,        // Thread tracing
    TRACE          // Fine-grained debugging
}
