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

namespace EmulationServer.Game.Maps.Runtime;

public sealed class MapServiceDefinition
{
    public int MapId { get; init; }

    public long InstanceId { get; init; }

    public string Name { get; init; } = string.Empty;

    public MapServiceKind Kind { get; init; }

    public TimeSpan TickInterval { get; init; } = TimeSpan.FromMilliseconds(100);

    public bool LogTicks { get; init; }

    public void Validate()
    {
        if (MapId < 0)
        {
            throw new InvalidOperationException("Map service map id must be greater than or equal to zero.");
        }

        if (InstanceId < 0)
        {
            throw new InvalidOperationException("Map service instance id must be greater than or equal to zero.");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException($"Map service {MapId} requires a display name.");
        }

        if (TickInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"Map service '{Name}' tick interval must be greater than zero.");
        }
    }
}
