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
// File: src/WorldServer/Configuration/RealmStatusSettings.cs
// Purpose: Contains realm status settings code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.WorldServer.Configuration;

// Type: RealmStatusSettings
// Purpose: Provides realm status settings behavior for the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class RealmStatusSettings
{

    // Property: Gets or sets the enabled value used by the world server gameplay, session, and character runtime layer.
    // Value: enabled value exposed by the owning type.
    public bool Enabled { get; init; } = true;

    // Property: Gets or sets the realm ID value used by the world server gameplay, session, and character runtime layer.
    // Value: realm ID value exposed by the owning type.
    public uint RealmId { get; init; } = 1;

    // Property: Gets or sets the realm server host value used by the world server gameplay, session, and character runtime layer.
    // Value: realm server host value exposed by the owning type.
    public string RealmServerHost { get; init; } = "127.0.0.1";

    // Property: Gets or sets the realm server port value used by the world server gameplay, session, and character runtime layer.
    // Value: realm server port value exposed by the owning type.
    public ushort RealmServerPort { get; init; } = 5005;

    // Method: FromSeconds
    // Purpose: Executes the from seconds operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the time span update interval { get; init; } = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to RealmStatusSettings so callers do not duplicate validation, protocol, or persistence rules.
    public TimeSpan UpdateInterval { get; init; } = TimeSpan.FromSeconds(15);

    // Property: Gets or sets the population capacity limit value used by the world server gameplay, session, and character runtime layer.
    // Value: population capacity limit value exposed by the owning type.
    public int PopulationCapacityLimit { get; init; }

    // Method: Validate
    // Purpose: Validates or evaluates validate rules for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmStatusSettings so callers do not duplicate validation, protocol, or persistence rules.
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

        if (PopulationCapacityLimit < 0)
        {
            throw new InvalidOperationException("Realm status population capacity limit cannot be negative.");
        }

    }
}
