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


/**
 * File overview: src/RealmServer/Realms/RealmPopulationCalculator.cs
 * Documents the RealmPopulationCalculator source file in the realm authentication, realm-list handling, and external client login services area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.RealmServer.Realms;

/**
 * Owns the realm population calculator behavior for the realm authentication, realm-list handling, and external client login services layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public static class RealmPopulationCalculator
{
    /**
      * Calculates a derived value from current runtime state.
      * The method is part of RealmPopulationCalculator and keeps this workflow isolated from the caller.
      */
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
