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
using EmulationServer.Network.Configuration;

using EmulationServer.Shared.Logging.Configuration;
/**
  * File overview: src/WorldServer/Configuration/WorldServerSettings.cs
  * This file belongs to the server configuration loading and strongly typed settings portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.WorldServer.Configuration;

/**
  * Represents the world server settings component in the server configuration loading and strongly typed settings area.
  * It keeps configuration values grouped by responsibility and prevents unrelated server code from reading raw INI keys.
  */
public sealed class WorldServerSettings
{
    /**
      * Gets or stores the logging value used by WorldServerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public LoggingSettings Logging { get; init; } = new();

    /**
      * Gets or stores the internal network value used by WorldServerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public InternalNetworkSettings InternalNetwork { get; init; } = new();

    /**
      * Gets or stores the max connections value used by WorldServerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int MaxConnections { get; init; } = 1000;

    /**
      * Gets the message sent to players after they enter the world.
      */
    public string MessageOfTheDay { get; init; } = "Welcome to Emulation Server.";

    /**
      * Gets how often active in-world player state is persisted while the player remains connected.
      */
    public TimeSpan PlayerSaveInterval { get; init; } = TimeSpan.FromSeconds(60);

    /**
      * Gets shared database connection defaults used to inherit host/user/pool settings for the concrete WorldServer schemas.
      * WorldServer does not open this database directly.
      */
    public DatabaseSettings Database { get; init; } = new();

    /**
      * Gets grouped database settings for the account/auth, character, and world schemas.
      */
    public WorldDatabaseSettings Databases { get; init; } = new();

    /**
      * Gets public WoW client listener settings for the realm connection port.
      */
    public WorldClientSettings ClientNetwork { get; init; } = new();

    /**
      * Gets or stores the realm status value used by WorldServerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public RealmStatusSettings RealmStatus { get; init; } = new();

    /**
      * Gets or stores the game data value used by WorldServerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public GameDataSettings GameData { get; init; } = new();

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of WorldServerSettings and keeps this workflow isolated from the caller.
      */
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
