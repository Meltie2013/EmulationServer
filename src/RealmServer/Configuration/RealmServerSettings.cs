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
namespace EmulationServer.RealmServer.Configuration;

public sealed class RealmServerSettings
{
    public LoggingSettings Logging { get; init; } = new();

    public RealmSocketListenerSettings Socket { get; init; } = new();

    public DatabaseSettings Database { get; init; } = new();

    public InternalNetworkSettings InternalNetwork { get; init; } = new();

    public IReadOnlyList<ConfiguredRealmSettings> Realms { get; init; } = [];

    public void Validate()
    {
        Logging.Validate();
        Socket.Validate();
        Database.Validate();
        InternalNetwork.Validate();

        if (Realms.Count == 0)
        {
            throw new InvalidOperationException("At least one realm must be configured.");
        }

        HashSet<uint> realmIds = [];
        foreach (ConfiguredRealmSettings realm in Realms)
        {
            realm.Validate();

            if (!realmIds.Add(realm.Id))
            {
                throw new InvalidOperationException($"Duplicate realm id configured: {realm.Id}.");
            }
        }
    }
}
