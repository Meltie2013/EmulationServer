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
// File: src/WorldServer/Configuration/WorldServerSettings.cs
// Purpose: Contains world server settings code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Database.Configuration;
using EmulationServer.Network.Configuration;

using EmulationServer.Shared.Logging.Configuration;

namespace EmulationServer.WorldServer.Configuration;

// Type: WorldServerSettings
// Purpose: Provides world server settings behavior for the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class WorldServerSettings
{

    public LoggingSettings Logging { get; init; } = new();

    public InternalNetworkSettings InternalNetwork { get; init; } = new();

    // Property: Gets or sets the max connections value used by the world server gameplay, session, and character runtime layer.
    // Value: max connections value exposed by the owning type.
    public int MaxConnections { get; init; } = 1000;

    // Property: Gets or sets the message of the day value used by the world server gameplay, session, and character runtime layer.
    // Value: message of the day value exposed by the owning type.
    public string MessageOfTheDay { get; init; } = "Welcome to Emulation Server.";

    // Method: FromSeconds
    // Purpose: Executes the from seconds operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the time span player save interval { get; init; } = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to WorldServerSettings so callers do not duplicate validation, protocol, or persistence rules.
    public TimeSpan PlayerSaveInterval { get; init; } = TimeSpan.FromSeconds(60);

    public DatabaseSettings Database { get; init; } = new();

    public WorldDatabaseSettings Databases { get; init; } = new();

    public WorldClientSettings ClientNetwork { get; init; } = new();

    public RealmStatusSettings RealmStatus { get; init; } = new();

    public GameDataSettings GameData { get; init; } = new();

    // Method: Validate
    // Purpose: Validates or evaluates validate rules for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldServerSettings so callers do not duplicate validation, protocol, or persistence rules.
    public void Validate()
    {
        Logging.Validate();
        InternalNetwork.Validate();

        if (MaxConnections <= 0)
        {
            throw new InvalidOperationException("WorldServer max connections must be greater than zero.");
        }

        if (PlayerSaveInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("WorldServer player save interval must be greater than zero.");
        }

        Database.Validate();
        Databases.Validate();
        ClientNetwork.Validate();
        RealmStatus.Validate();
        GameData.Validate();
    }
}
