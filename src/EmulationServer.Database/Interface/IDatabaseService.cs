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

using MySqlConnector;

/**
  * File overview: src/EmulationServer.Database/Interface/IDatabaseService.cs
  * Documents the IDatabaseService source file in the database access, account persistence, and MySQL connectivity area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Database.Interfaces;

/**
  * Defines the contract for database service behavior in the database access, account persistence, and MySQL connectivity layer.
  * Implementations are expected to keep caller-facing behavior stable because other servers depend on this shape across shared game and network workflows.
  */
public interface IDatabaseService : IAsyncDisposable
{
    /**
      * Creates the create connection async resource for the implementing service.
      * Callers use the contract method so gameplay, database, and network code can depend on behavior rather than a concrete implementation.
      */
    ValueTask<MySqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);

    /**
      * Runs the test connection async check through the implementing service.
      * Callers use the contract method so gameplay, database, and network code can depend on behavior rather than a concrete implementation.
      */
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /**
      * Runs the validate connection async check through the implementing service.
      * Callers use the contract method so gameplay, database, and network code can depend on behavior rather than a concrete implementation.
      */
    Task ValidateConnectionAsync(CancellationToken cancellationToken = default);
}
