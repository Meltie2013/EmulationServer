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
// File: src/WorldServer/Configuration/WorldServerConfigurationLoader.cs
// Purpose: Contains world server configuration loader code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Core.Configuration;
using EmulationServer.Database.Configuration;
using EmulationServer.Shared.Configuration;

namespace EmulationServer.WorldServer.Configuration;

// Type: WorldServerConfigurationLoader
// Purpose: Provides world server configuration loader behavior for the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class WorldServerConfigurationLoader
{

    // Constant: Defines the world server section constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed world server section value used anywhere this rule or protocol value is needed.
    private const string WorldServerSection = "WorldServer";

    // Constant: Defines the realm status section constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed realm status section value used anywhere this rule or protocol value is needed.
    private const string RealmStatusSection = "RealmStatus";

    // Constant: Defines the game data section constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed game data section value used anywhere this rule or protocol value is needed.
    private const string GameDataSection = "GameData";

    // Constant: Defines the world client section constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed world client section value used anywhere this rule or protocol value is needed.
    private const string WorldClientSection = "WorldClient";

    // Constant: Defines the auth database section constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed auth database section value used anywhere this rule or protocol value is needed.
    private const string AuthDatabaseSection = "AuthDatabase";

    // Constant: Defines the character database section constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed character database section value used anywhere this rule or protocol value is needed.
    private const string CharacterDatabaseSection = "CharacterDatabase";

    // Constant: Defines the world database section constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed world database section value used anywhere this rule or protocol value is needed.
    private const string WorldDatabaseSection = "WorldDatabase";

    // Method: Load
    // Purpose: Retrieves load data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - path: Path value supplied by the caller for this operation.
    // Returns: Returns the world server settings value produced by this operation.
    // Notes: This keeps the operation scoped to WorldServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: LoadRealmStatusSettings
    // Purpose: Retrieves load realm status settings data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - configuration: Configuration values that control how this operation should run.
    // Returns: Returns the realm status settings value produced by this operation.
    // Notes: This keeps the operation scoped to WorldServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
    private static RealmStatusSettings LoadRealmStatusSettings(IniConfiguration configuration)
    {
        return new RealmStatusSettings
        {
            Enabled = configuration.GetBool(RealmStatusSection, "Enabled", true),
            RealmId = (uint)configuration.GetInt(RealmStatusSection, "RealmId", 1, minimum: 1),
            RealmServerHost = configuration.GetString(RealmStatusSection, "RealmServerHost", "127.0.0.1"),
            RealmServerPort = (ushort)configuration.GetInt(RealmStatusSection, "RealmServerPort", 5005, minimum: 1, maximum: 65535),
            UpdateInterval = configuration.GetTimeSpan(RealmStatusSection, "UpdateInterval", TimeSpan.FromSeconds(15)),
            PopulationCapacityLimit = configuration.GetInt(RealmStatusSection, "PopulationCapacityLimit", 0, minimum: 0),
        };
    }

    // Method: LoadWorldClientSettings
    // Purpose: Retrieves load world client settings data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - configuration: Configuration values that control how this operation should run.
    // Returns: Returns the world client settings value produced by this operation.
    // Notes: This keeps the operation scoped to WorldServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: LoadWorldDatabaseSettings
    // Purpose: Retrieves load world database settings data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - configuration: Configuration values that control how this operation should run.
    // Returns: Returns the world database settings value produced by this operation.
    // Notes: This keeps the operation scoped to WorldServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: LoadDatabaseSettings
    // Purpose: Retrieves load database settings data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - configuration: Configuration values that control how this operation should run.
    // - sectionName: Section name value supplied by the caller for this operation.
    // - fallback: Fallback value supplied by the caller for this operation.
    // - defaultDatabaseName: Default database name value supplied by the caller for this operation.
    // Returns: Returns the database settings value produced by this operation.
    // Notes: This keeps the operation scoped to WorldServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: LoadGameDataSettings
    // Purpose: Retrieves load game data settings data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - configuration: Configuration values that control how this operation should run.
    // Returns: Returns the game data settings value produced by this operation.
    // Notes: This keeps the operation scoped to WorldServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
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
            MapStoreDirectory = configuration.GetString(GameDataSection, "MapStoreDirectory", "mapstore"),
            RequiredDbcFiles = SplitList(requiredDbcFiles).ToArray(),
        };
    }

    // Method: SplitList
    // Purpose: Executes the split list operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the I enumerable value produced by this operation.
    // Notes: This keeps the operation scoped to WorldServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
    private static IEnumerable<string> SplitList(string value)
    {
        return value.Split([';', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}
