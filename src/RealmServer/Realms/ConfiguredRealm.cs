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

using EmulationServer.RealmServer.Configuration;

namespace EmulationServer.RealmServer.Realms;

public sealed class ConfiguredRealm
{
    private readonly object _syncRoot = new();

    private bool _online;
    private int _activeConnections;
    private int _capacityLimit;

    public ConfiguredRealm(ConfiguredRealmSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        Id = settings.Id;
        Name = settings.Name;
        Address = settings.Address;
        Port = settings.Port;
        Icon = settings.Icon;
        BaseRealmFlags = settings.RealmFlags;
        Timezone = settings.Timezone;
        AllowedSecurityLevel = settings.AllowedSecurityLevel;
        Builds = settings.Builds;

        _online = settings.Online;
        _activeConnections = settings.ActiveConnections;
        _capacityLimit = 1;
    }

    public uint Id { get; }

    public string Name { get; }

    public string Address { get; }

    public ushort Port { get; }

    public byte Icon { get; }

    public byte BaseRealmFlags { get; }

    public byte Timezone { get; }

    public byte AllowedSecurityLevel { get; }

    public IReadOnlySet<ushort> Builds { get; }

    public bool IsOnline
    {
        get
        {
            lock (_syncRoot)
            {
                return _online;
            }
        }
    }

    public int ActiveConnections
    {
        get
        {
            lock (_syncRoot)
            {
                return _activeConnections;
            }
        }
    }

    public int CapacityLimit
    {
        get
        {
            lock (_syncRoot)
            {
                return _capacityLimit;
            }
        }
    }

    public float Population
    {
        get
        {
            lock (_syncRoot)
            {
                return RealmPopulationCalculator.Calculate(_activeConnections, _capacityLimit);
            }
        }
    }

    public string ClientAddress => $"{Address}:{Port}";

    public void SetStatus(bool online, int activeConnections, int capacityLimit)
    {
        lock (_syncRoot)
        {
            _online = online;
            _activeConnections = Math.Max(0, activeConnections);
            _capacityLimit = Math.Max(1, capacityLimit);
        }
    }
}
