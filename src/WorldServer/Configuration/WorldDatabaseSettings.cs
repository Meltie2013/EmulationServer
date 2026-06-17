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
// File: src/WorldServer/Configuration/WorldDatabaseSettings.cs
// Purpose: Contains world database settings code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Database.Configuration;

namespace EmulationServer.WorldServer.Configuration;

// Type: WorldDatabaseSettings
// Purpose: Provides world database settings behavior for the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class WorldDatabaseSettings
{

    public DatabaseSettings Auth { get; init; } = new() { Database = "account" };

    public DatabaseSettings Character { get; init; } = new() { Database = "character0" };

    public DatabaseSettings World { get; init; } = new() { Database = "mangos0" };

    // Method: Validate
    // Purpose: Validates or evaluates validate rules for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldDatabaseSettings so callers do not duplicate validation, protocol, or persistence rules.
    public void Validate()
    {
        Auth.Validate();
        Character.Validate();
        World.Validate();
    }
}
