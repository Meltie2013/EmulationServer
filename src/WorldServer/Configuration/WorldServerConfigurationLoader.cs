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
using EmulationServer.Database.Configuration;
using EmulationServer.Shared.Configuration;


/**
 * File overview: src/WorldServer/Configuration/WorldServerConfigurationLoader.cs
 * Documents the WorldServerConfigurationLoader source file in the world server configuration and startup settings area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.WorldServer.Configuration;

/**
 * Owns the world server configuration loader behavior for the world server configuration and startup settings layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public static class WorldServerConfigurationLoader
{
    /**
     * Defines the constant value for world server section.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    private const string WorldServerSection = "WorldServer";
    /**
     * Defines the constant value for realm status section.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    private const string RealmStatusSection = "RealmStatus";
    /**
     * Defines the constant value for game data section.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    private const string GameDataSection = "GameData";
    /**
     * Defines the constant value for world client section.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    private const string WorldClientSection = "WorldClient";
    /**
     * Defines the constant value for auth database section.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    private const string AuthDatabaseSection = "AuthDatabase";
    /**
     * Defines the constant value for character database section.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    private const string CharacterDatabaseSection = "CharacterDatabase";
    /**
     * Defines the constant value for world database section.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    private const string WorldDatabaseSection = "WorldDatabase";

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
            MessageOfTheDay = configuration.GetString(WorldServerSection, "MessageOfTheDay", "Welcome to Emulation Server."),
            PlayerSaveInterval = configuration.GetTimeSpan(WorldServerSection, "PlayerSaveInterval", TimeSpan.FromSeconds(60)),
            Database = ServerConfigurationLoader.LoadDatabaseSettings(configuration),
            Databases = LoadWorldDatabaseSettings(configuration),
            ClientNetwork = LoadWorldClientSettings(configuration),
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
      * Loads public WoW client socket settings.
      */
    private static WorldClientSettings LoadWorldClientSettings(IniConfiguration configuration)
    {
        return new WorldClientSettings
        {
            BindAddress = configuration.GetString(WorldClientSection, "BindAddress", "127.0.0.1"),
            Port = (ushort)configuration.GetInt(WorldClientSection, "Port", 8085, minimum: 1, maximum: 65535),
            Backlog = configuration.GetInt(WorldClientSection, "Backlog", 128, minimum: 1),
            ReceiveBufferSize = configuration.GetInt(WorldClientSection, "ReceiveBufferSize", 65536, minimum: 1024),
            SendBufferSize = configuration.GetInt(WorldClientSection, "SendBufferSize", 65536, minimum: 1024),
            KeepAlive = configuration.GetBool(WorldClientSection, "KeepAlive", true),
            KeepAliveTimeSeconds = configuration.GetInt(WorldClientSection, "KeepAliveTimeSeconds", 30, minimum: 0),
            KeepAliveIntervalSeconds = configuration.GetInt(WorldClientSection, "KeepAliveIntervalSeconds", 10, minimum: 0),
            ShutdownGracePeriod = configuration.GetTimeSpan(WorldClientSection, "ShutdownGracePeriod", TimeSpan.FromSeconds(15)),
            MaximumPacketSize = configuration.GetInt(WorldClientSection, "MaximumPacketSize", 0x8000, minimum: 6),
        };
    }

    /**
     * Loads load world database settings information from configuration, files, or persistent storage.
     * The method normalizes external input before returning it so the rest of the server can work with validated, strongly typed data.
     * Inputs used by this operation: configuration.
     */
    private static WorldDatabaseSettings LoadWorldDatabaseSettings(IniConfiguration configuration)
    {
        DatabaseSettings fallback = ServerConfigurationLoader.LoadDatabaseSettings(configuration);

        return new WorldDatabaseSettings
        {
            Auth = LoadDatabaseSettings(configuration, AuthDatabaseSection, fallback, "account"),
            Character = LoadDatabaseSettings(configuration, CharacterDatabaseSection, fallback, "character0"),
            World = LoadDatabaseSettings(configuration, WorldDatabaseSection, fallback, "mangos0"),
        };
    }

    /**
      * Loads a database section while inheriting connection host/user settings from [Database].
      */
    private static DatabaseSettings LoadDatabaseSettings(
        IniConfiguration configuration,
        string sectionName,
        DatabaseSettings fallback,
        string defaultDatabaseName)
    {
        return new DatabaseSettings
        {
            Host = configuration.GetString(sectionName, "Host", fallback.Host),
            Port = configuration.GetInt(sectionName, "Port", fallback.Port, minimum: 1, maximum: 65535),
            Database = configuration.GetString(sectionName, "Database", defaultDatabaseName),
            Username = configuration.GetString(sectionName, "Username", fallback.Username),
            Password = configuration.GetString(sectionName, "Password", fallback.Password),
            MinimumPoolSize = (uint)configuration.GetInt(sectionName, "MinimumPoolSize", (int)fallback.MinimumPoolSize, minimum: 0),
            MaximumPoolSize = (uint)configuration.GetInt(sectionName, "MaximumPoolSize", (int)fallback.MaximumPoolSize, minimum: 1),
            UseSsl = configuration.GetBool(sectionName, "UseSsl", fallback.UseSsl),
            ConnectionTimeoutSeconds = (uint)configuration.GetInt(sectionName, "ConnectionTimeoutSeconds", (int)fallback.ConnectionTimeoutSeconds, minimum: 1),
            DefaultCommandTimeoutSeconds = (uint)configuration.GetInt(sectionName, "DefaultCommandTimeoutSeconds", (int)fallback.DefaultCommandTimeoutSeconds, minimum: 1),
            ConnectionIdleTimeoutSeconds = (uint)configuration.GetInt(sectionName, "ConnectionIdleTimeoutSeconds", (int)fallback.ConnectionIdleTimeoutSeconds, minimum: 1),
            ConnectionLifeTimeSeconds = (uint)configuration.GetInt(sectionName, "ConnectionLifeTimeSeconds", (int)fallback.ConnectionLifeTimeSeconds, minimum: 0),
            KeepAliveSeconds = (uint)configuration.GetInt(sectionName, "KeepAliveSeconds", (int)fallback.KeepAliveSeconds, minimum: 0),
            ConnectionReset = configuration.GetBool(sectionName, "ConnectionReset", fallback.ConnectionReset),
            UseCompression = configuration.GetBool(sectionName, "UseCompression", fallback.UseCompression),
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
            Enabled = configuration.GetBool(GameDataSection, "Enabled", true),
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
