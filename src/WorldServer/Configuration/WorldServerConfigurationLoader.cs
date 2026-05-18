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

using EmulationServer.Core.Configuration;
using EmulationServer.Shared.Configuration;

/**
  * File overview: src/WorldServer/Configuration/WorldServerConfigurationLoader.cs
  * This file belongs to the server configuration loading and strongly typed settings portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.WorldServer.Configuration;

/**
  * Represents the world server configuration loader component in the server configuration loading and strongly typed settings area.
  * It centralizes INI parsing so startup code can work with strongly typed settings instead of raw strings.
  */
public static class WorldServerConfigurationLoader
{
    private const string WorldServerSection = "WorldServer";
    private const string RealmStatusSection = "RealmStatus";
    private const string GameDataSection = "GameData";

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of WorldServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    public static WorldServerSettings Load(string path)
    {
        string fullPath = Path.GetFullPath(path);
        IniConfiguration configuration = IniConfiguration.Load(fullPath);

        WorldServerSettings settings = new()
        {
            Logging = ServerConfigurationLoader.LoadLoggingSettings(configuration, fullPath, "WorldServer"),

            InternalNetwork = ServerConfigurationLoader.LoadInternalNetworkSettings(
                configuration,
                WorldServerSection,
                "WorldServer",
                5002),
            MaxConnections = configuration.GetInt(WorldServerSection, "MaxConnections", 1000, minimum: 1),
            Database = ServerConfigurationLoader.LoadDatabaseSettings(configuration),
            RealmStatus = LoadRealmStatusSettings(configuration),
            GameData = LoadGameDataSettings(configuration),
        };

        settings.Validate();

        return settings;
    }

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of WorldServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static RealmStatusSettings LoadRealmStatusSettings(IniConfiguration configuration)
    {
        return new RealmStatusSettings
        {
            Enabled = configuration.GetBool(RealmStatusSection, "Enabled", true),
            RealmId = (uint)configuration.GetInt(RealmStatusSection, "RealmId", 1, minimum: 1),
            RealmServerHost = configuration.GetString(RealmStatusSection, "RealmServerHost", "127.0.0.1"),
            RealmServerPort = (ushort)configuration.GetInt(RealmStatusSection, "RealmServerPort", 5005, minimum: 1, maximum: 65535),
            UpdateInterval = configuration.GetTimeSpan(RealmStatusSection, "UpdateInterval", TimeSpan.FromSeconds(15)),
        };
    }


    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of WorldServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static GameDataSettings LoadGameDataSettings(IniConfiguration configuration)
    {
        string requiredDbcFiles = configuration.GetString(
            GameDataSection,
            "RequiredDbcFiles",
            string.Join(';', GameDataSettings.DefaultRequiredDbcFiles));

        return new GameDataSettings
        {
            Enabled = configuration.GetBool(GameDataSection, "Enabled", false),
            DataDirectory = configuration.GetString(GameDataSection, "DataDirectory", "Data"),
            DbcDirectory = configuration.GetString(GameDataSection, "DbcDirectory", "dbc"),
            RequiredDbcFiles = SplitList(requiredDbcFiles).ToArray(),
        };
    }

    /**
      * Splits the supplied text into command parts while preserving quoted values.
      * The method is part of WorldServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static IEnumerable<string> SplitList(string value)
    {
        return value.Split([';', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}
