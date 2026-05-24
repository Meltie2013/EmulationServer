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
 * File overview: src/EmulationServer.Shared/Logging/Interface/ILogger.cs
 * Documents the ILogger source file in the shared configuration, logging, and utility support area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Shared.Logging.Interfaces;

/**
 * Defines the contract for logger behavior in the shared configuration, logging, and utility support layer.
 * Implementations are expected to keep caller-facing behavior stable because other servers depend on this shape across shared game and network workflows.
 */
public interface ILogger
{
    /**
     * Performs the write operation through the implementing contract.
     * Callers use the contract method so gameplay, database, and network code can depend on behavior rather than a concrete implementation.
     */
    void Write(LogType type, string message, string? category = null);
}
