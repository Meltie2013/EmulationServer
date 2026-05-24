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

using EmulationServer.Database.Configuration;

/**
 * File overview: src/WorldServer/Configuration/WorldDatabaseSettings.cs
 * Documents the WorldDatabaseSettings source file in the world server configuration and startup settings area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.WorldServer.Configuration;

/**
 * Owns the world database settings behavior for the world server configuration and startup settings layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public sealed class WorldDatabaseSettings
{
    /**
     * Exposes the auth value to callers that need this runtime or configuration data.
     * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
     */
    public DatabaseSettings Auth { get; init; } = new() { Database = "account" };
    /**
     * Exposes the character value to callers that need this runtime or configuration data.
     * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
     */
    public DatabaseSettings Character { get; init; } = new() { Database = "character0" };
    /**
     * Exposes the world value to callers that need this runtime or configuration data.
     * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
     */
    public DatabaseSettings World { get; init; } = new() { Database = "mangos0" };

    /**
     * Validates validate state before it is used by another server component.
     * Validation failures are raised as close to the source as possible so configuration, packet, and data problems are easier to diagnose.
     */
    public void Validate()
    {
        Auth.Validate();
        Character.Validate();
        World.Validate();
    }
}
