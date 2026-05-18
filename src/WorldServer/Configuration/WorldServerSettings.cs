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
namespace EmulationServer.WorldServer.Configuration;

public sealed class WorldServerSettings
{
    public LoggingSettings Logging { get; init; } = new();

    public InternalNetworkSettings InternalNetwork { get; init; } = new();

    public int MaxConnections { get; init; } = 1000;

    public DatabaseSettings Database { get; init; } = new();

    public RealmStatusSettings RealmStatus { get; init; } = new();

    public GameDataSettings GameData { get; init; } = new();

    public void Validate()
    {
        Logging.Validate();
        InternalNetwork.Validate();

        if (MaxConnections <= 0)
        {
            throw new InvalidOperationException("WorldServer max connections must be greater than zero.");
        }

        Database.Validate();
        RealmStatus.Validate();
        GameData.Validate();
    }
}
