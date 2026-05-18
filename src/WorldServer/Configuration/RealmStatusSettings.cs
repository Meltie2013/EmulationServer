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

namespace EmulationServer.WorldServer.Configuration;

public sealed class RealmStatusSettings
{
    public bool Enabled { get; init; } = true;

    public uint RealmId { get; init; } = 1;

    public string RealmServerHost { get; init; } = "127.0.0.1";

    public ushort RealmServerPort { get; init; } = 5005;

    public TimeSpan UpdateInterval { get; init; } = TimeSpan.FromSeconds(15);


    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }

        if (RealmId == 0)
        {
            throw new InvalidOperationException("Realm status realm id must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(RealmServerHost))
        {
            throw new InvalidOperationException("Realm status RealmServer host is required.");
        }

        if (RealmServerPort == 0)
        {
            throw new InvalidOperationException("Realm status RealmServer port must be greater than zero.");
        }

        if (UpdateInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Realm status update interval must be greater than zero.");
        }

    }
}
