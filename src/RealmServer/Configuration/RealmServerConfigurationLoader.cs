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
// File: src/RealmServer/Configuration/RealmServerConfigurationLoader.cs
// Purpose: Contains realm server configuration loader code for the realm server authentication, realm-list, and account connection layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Globalization;

using EmulationServer.Core.Configuration;
using EmulationServer.Database.Configuration;
using EmulationServer.Network.Configuration;
using EmulationServer.RealmServer.Realms;
using EmulationServer.Shared.Configuration;

namespace EmulationServer.RealmServer.Configuration;

// Type: RealmServerConfigurationLoader
// Purpose: Provides realm server configuration loader behavior for the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class RealmServerConfigurationLoader
{

    // Constant: Defines the realm server section constant used by the realm server authentication, realm-list, and account connection layer.
    // Value: fixed realm server section value used anywhere this rule or protocol value is needed.
    private const string RealmServerSection = "RealmServer";

    // Constant: Defines the database section constant used by the realm server authentication, realm-list, and account connection layer.
    // Value: fixed database section value used anywhere this rule or protocol value is needed.
    private const string DatabaseSection = "Database";

    // Constant: Defines the realms section constant used by the realm server authentication, realm-list, and account connection layer.
    // Value: fixed realms section value used anywhere this rule or protocol value is needed.
    private const string RealmsSection = "Realms";

    // Constant: Defines the realm list section constant used by the realm server authentication, realm-list, and account connection layer.
    // Value: fixed realm list section value used anywhere this rule or protocol value is needed.
    private const string RealmListSection = "RealmList";

    // Constant: Defines the internal network section constant used by the realm server authentication, realm-list, and account connection layer.
    // Value: fixed internal network section value used anywhere this rule or protocol value is needed.
    private const string InternalNetworkSection = "InternalNetwork";

    // Method: Load
    // Purpose: Retrieves load data for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - path: Path value supplied by the caller for this operation.
    // Returns: Returns the realm server settings value produced by this operation.
    // Notes: This keeps the operation scoped to RealmServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
    public static RealmServerSettings Load(string path)
    {
        string fullPath = Path.GetFullPath(path);

        IniConfiguration configuration = IniConfiguration.Load(fullPath);

        RealmServerSettings settings = new()
        {
            Logging = ServerConfigurationLoader.LoadLoggingSettings(configuration, fullPath, "RealmServer"),

            Socket = LoadSocketSettings(configuration),
            Database = LoadDatabaseSettings(configuration),
            InternalNetwork = ServerConfigurationLoader.LoadInternalNetworkSettings(
                configuration,
                InternalNetworkSection,
                "RealmServer",
                5005),
            RealmList = LoadRealmListSettings(configuration),
            Realms = LoadRealmSettings(configuration),
        };

        settings.Validate();

        return settings;
    }

    // Method: LoadSocketSettings
    // Purpose: Retrieves load socket settings data for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - configuration: Configuration values that control how this operation should run.
    // Returns: Returns the realm socket listener settings value produced by this operation.
    // Notes: This keeps the operation scoped to RealmServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
    private static RealmSocketListenerSettings LoadSocketSettings(IniConfiguration configuration)
    {
        return new RealmSocketListenerSettings
        {
            BindAddress = configuration.GetString(
                RealmServerSection,
                "BindAddress",
                "0.0.0.0"),

            Port = configuration.GetInt(
                RealmServerSection,
                "Port",
                3724,
                minimum: 0,
                maximum: 65535),

            Backlog = configuration.GetInt(
                RealmServerSection,
                "Backlog",
                128,
                minimum: 1),

            ReceiveBufferSize = configuration.GetInt(
                RealmServerSection,
                "ReceiveBufferSize",
                65536,
                minimum: 1024),

            SendBufferSize = configuration.GetInt(
                RealmServerSection,
                "SendBufferSize",
                65536,
                minimum: 1024),

            KeepAlive = configuration.GetBool(
                RealmServerSection,
                "KeepAlive",
                true),

            KeepAliveTimeSeconds = configuration.GetInt(
                RealmServerSection,
                "KeepAliveTimeSeconds",
                30,
                minimum: 0),

            KeepAliveIntervalSeconds = configuration.GetInt(
                RealmServerSection,
                "KeepAliveIntervalSeconds",
                10,
                minimum: 0),

            ShutdownGracePeriod = configuration.GetTimeSpan(
                RealmServerSection,
                "ShutdownGracePeriod",
                TimeSpan.FromSeconds(15)),
        };
    }

    // Method: LoadDatabaseSettings
    // Purpose: Retrieves load database settings data for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - configuration: Configuration values that control how this operation should run.
    // Returns: Returns the database settings value produced by this operation.
    // Notes: This keeps the operation scoped to RealmServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
    private static DatabaseSettings LoadDatabaseSettings(IniConfiguration configuration)
    {
        return new DatabaseSettings
        {
            Host = configuration.GetString(
                DatabaseSection,
                "Host",
                "127.0.0.1"),

            Port = configuration.GetInt(
                DatabaseSection,
                "Port",
                3306,
                minimum: 1,
                maximum: 65535),

            Database = configuration.GetString(
                DatabaseSection,
                "Database",
                "realmd"),

            Username = configuration.GetString(
                DatabaseSection,
                "Username",
                "root"),

            Password = configuration.GetString(
                DatabaseSection,
                "Password",
                ""),

            MinimumPoolSize = configuration.GetUInt(
                DatabaseSection,
                "MinimumPoolSize",
                5),

            MaximumPoolSize = configuration.GetUInt(
                DatabaseSection,
                "MaximumPoolSize",
                100,
                minimum: 1),

            UseSsl = configuration.GetBool(
                DatabaseSection,
                "UseSsl",
                false),

            ConnectionTimeoutSeconds = configuration.GetUInt(
                DatabaseSection,
                "ConnectionTimeoutSeconds",
                15,
                minimum: 1),

            DefaultCommandTimeoutSeconds = configuration.GetUInt(
                DatabaseSection,
                "DefaultCommandTimeoutSeconds",
                30,
                minimum: 1),

            ConnectionIdleTimeoutSeconds = configuration.GetUInt(
                DatabaseSection,
                "ConnectionIdleTimeoutSeconds",
                180,
                minimum: 1),

            ConnectionLifeTimeSeconds = configuration.GetUInt(
                DatabaseSection,
                "ConnectionLifeTimeSeconds",
                0),

            KeepAliveSeconds = configuration.GetUInt(
                DatabaseSection,
                "KeepAliveSeconds",
                30),

            ConnectionReset = configuration.GetBool(
                DatabaseSection,
                "ConnectionReset",
                true),

            UseCompression = configuration.GetBool(
                DatabaseSection,
                "UseCompression",
                false),
        };
    }

    // Method: LoadRealmListSettings
    // Purpose: Retrieves load realm list settings data for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - configuration: Configuration values that control how this operation should run.
    // Returns: Returns the realm list settings value produced by this operation.
    // Notes: This keeps the operation scoped to RealmServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
    private static RealmListSettings LoadRealmListSettings(IniConfiguration configuration)
    {
        return new RealmListSettings
        {
            RequireWorldServerStatus = configuration.GetBool(
                RealmListSection,
                "RequireWorldServerStatus",
                true),

            HideStaleRealms = configuration.GetBool(
                RealmListSection,
                "HideStaleRealms",
                true),

            StaleRealmTimeout = configuration.GetTimeSpan(
                RealmListSection,
                "StaleRealmTimeout",
                TimeSpan.FromMinutes(5)),
        };
    }

    // Method: LoadRealmSettings
    // Purpose: Retrieves load realm settings data for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - configuration: Configuration values that control how this operation should run.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to RealmServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
    private static IReadOnlyList<ConfiguredRealmSettings> LoadRealmSettings(IniConfiguration configuration)
    {
        string ids = configuration.GetString(RealmsSection, "RealmIds", "1");
        List<ConfiguredRealmSettings> realms = [];

        foreach (string idText in SplitList(ids))
        {
            if (!uint.TryParse(idText, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint realmId))
            {
                throw new ConfigurationException($"Invalid realm id in [{RealmsSection}] RealmIds: '{idText}'.");
            }

            string section = $"Realm.{realmId}";
            string builds = configuration.GetString(section, "Builds", "5875;6005;6141");

            realms.Add(new ConfiguredRealmSettings
            {
                Id = realmId,
                Name = configuration.GetString(section, "Name", "Emulation Server"),
                Address = configuration.GetString(section, "Address", "127.0.0.1"),
                Port = (ushort)configuration.GetInt(section, "Port", 8085, minimum: 1, maximum: 65535),
                Icon = (byte)configuration.GetInt(section, "Icon", 0, minimum: 0, maximum: byte.MaxValue),
                RealmFlags = ParseRealmFlags(configuration.GetString(section, "RealmFlags", "0"), section),
                Timezone = (byte)configuration.GetInt(section, "Timezone", 1, minimum: 0, maximum: byte.MaxValue),
                AllowedSecurityLevel = (byte)configuration.GetInt(section, "AllowedSecurityLevel", 0, minimum: 0, maximum: byte.MaxValue),
                Online = configuration.GetBool(section, "Online", false),
                ActiveConnections = configuration.GetInt(section, "ActiveConnections", 0, minimum: 0),
                Builds = ParseBuilds(builds, section),
            });
        }

        return realms;
    }

    // Method: ParseRealmFlags
    // Purpose: Converts incoming data into parse realm flags form for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // - section: Section value supplied by the caller for this operation.
    // Returns: Returns the realm flags value produced by this operation.
    // Notes: This keeps the operation scoped to RealmServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
    private static RealmFlags ParseRealmFlags(string value, string section)
    {
        try
        {
            return RealmFlagUtilities.ParseConfigurationValue(value);
        }
        catch (InvalidOperationException ex)
        {
            throw new ConfigurationException($"Invalid realm flags in [{section}] RealmFlags: {ex.Message}", ex);
        }
    }

    // Method: ParseBuilds
    // Purpose: Converts incoming data into parse builds form for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // - section: Section value supplied by the caller for this operation.
    // Returns: Returns the I read only set value produced by this operation.
    // Notes: This keeps the operation scoped to RealmServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
    private static IReadOnlySet<ushort> ParseBuilds(string value, string section)
    {
        HashSet<ushort> builds = [];

        foreach (string buildText in SplitList(value))
        {
            if (!ushort.TryParse(buildText, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort build))
            {
                throw new ConfigurationException($"Invalid client build in [{section}] Builds: '{buildText}'.");
            }

            builds.Add(build);
        }

        return builds;
    }

    // Method: SplitList
    // Purpose: Executes the split list operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the I enumerable value produced by this operation.
    // Notes: This keeps the operation scoped to RealmServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
    private static IEnumerable<string> SplitList(string value)
    {
        return value.Split([';', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}
