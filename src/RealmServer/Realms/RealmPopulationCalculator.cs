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
// File: src/RealmServer/Realms/RealmPopulationCalculator.cs
// Purpose: Contains realm population calculator code for the realm server authentication, realm-list, and account connection layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.RealmServer.Realms;

// Type: RealmPopulationCalculator
// Purpose: Provides realm population calculator behavior for the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class RealmPopulationCalculator
{

    // Method: Calculate
    // Purpose: Calculates calculate values for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - activeConnections: Active connections value supplied by the caller for this operation.
    // - capacityLimit: Capacity limit value supplied by the caller for this operation.
    // Returns: Returns the float value produced by this operation.
    // Notes: This keeps the operation scoped to RealmPopulationCalculator so callers do not duplicate validation, protocol, or persistence rules.
    public static float Calculate(int activeConnections, int capacityLimit)
    {
        if (activeConnections <= 0 || capacityLimit <= 0)
        {
            return 0.0f;
        }

        float population = (float)activeConnections / capacityLimit * 2.0f;

        return Math.Clamp(population, 0.0f, 2.0f);
    }
}
