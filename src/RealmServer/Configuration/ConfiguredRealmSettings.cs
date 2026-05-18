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

namespace EmulationServer.RealmServer.Configuration;

public sealed class ConfiguredRealmSettings
{
    public uint Id { get; init; }

    public string Name { get; init; } = "Emulation Server";

    public string Address { get; init; } = "127.0.0.1";

    public ushort Port { get; init; } = 8085;

    public byte Icon { get; init; }

    public byte RealmFlags { get; init; }

    public byte Timezone { get; init; } = 1;

    public byte AllowedSecurityLevel { get; init; }

    public bool Online { get; init; }

    public int ActiveConnections { get; init; }


    public IReadOnlySet<ushort> Builds { get; init; } = new HashSet<ushort>
    {
        5875,
        6005,
        6141,
        8606,
        12340,
        15595
    };

    public void Validate()
    {
        if (Id == 0)
        {
            throw new InvalidOperationException("Realm id must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException($"Realm {Id} name is required.");
        }

        if (string.IsNullOrWhiteSpace(Address))
        {
            throw new InvalidOperationException($"Realm {Id} address is required.");
        }

        if (Port == 0)
        {
            throw new InvalidOperationException($"Realm {Id} port is required.");
        }

        if (ActiveConnections < 0)
        {
            throw new InvalidOperationException($"Realm {Id} active connections cannot be negative.");
        }


        if (Builds.Count == 0)
        {
            throw new InvalidOperationException($"Realm {Id} must allow at least one client build.");
        }
    }
}
