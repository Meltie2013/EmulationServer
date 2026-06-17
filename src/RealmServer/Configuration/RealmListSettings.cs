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
// File: src/RealmServer/Configuration/RealmListSettings.cs
// Purpose: Contains realm list settings code for the realm server authentication, realm-list, and account connection layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.RealmServer.Configuration;

// Type: RealmListSettings
// Purpose: Provides realm list settings behavior for the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class RealmListSettings
{

    // Property: Gets or sets the require world server status value used by the realm server authentication, realm-list, and account connection layer.
    // Value: require world server status value exposed by the owning type.
    public bool RequireWorldServerStatus { get; init; } = true;

    // Property: Gets or sets the hide stale realms value used by the realm server authentication, realm-list, and account connection layer.
    // Value: hide stale realms value exposed by the owning type.
    public bool HideStaleRealms { get; init; } = true;

    // Method: FromMinutes
    // Purpose: Executes the from minutes operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: Returns the time span stale realm timeout { get; init; } = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to RealmListSettings so callers do not duplicate validation, protocol, or persistence rules.
    public TimeSpan StaleRealmTimeout { get; init; } = TimeSpan.FromMinutes(5);

    // Method: Validate
    // Purpose: Validates or evaluates validate rules for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmListSettings so callers do not duplicate validation, protocol, or persistence rules.
    public void Validate()
    {
        if (HideStaleRealms && StaleRealmTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Realm list stale realm timeout must be greater than zero when stale realm hiding is enabled.");
        }
    }
}
