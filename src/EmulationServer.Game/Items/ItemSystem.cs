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
// File: src/EmulationServer.Game/Items/ItemSystem.cs
// Purpose: Contains item system code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Game.WorldData;

namespace EmulationServer.Game.Items;

// Type: ItemSystem
// Purpose: Provides item system behavior for the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - worldTemplateAccessor: World template accessor value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class ItemSystem(Func<WorldTemplateDataStore> worldTemplateAccessor)
{

    // Method: ArgumentNullException
    // Purpose: Executes the argument null exception operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the func world template accessor = world template accessor ?? throw new value produced by this operation.
    // Notes: This keeps the operation scoped to ItemSystem so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Func<WorldTemplateDataStore> _worldTemplateAccessor = worldTemplateAccessor ?? throw new ArgumentNullException();

    // Method: TryGetItemTemplate
    // Purpose: Attempts to retrieve or parse try get item template data without treating normal misses as failures.
    // Parameters:
    // - entry: Entry value supplied by the caller for this operation.
    // - itemTemplate: Item template value supplied by the caller for this operation.
    // Returns: Returns true when try get item template succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to ItemSystem so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetItemTemplate(uint entry, out ItemTemplateRecord itemTemplate)
    {
        return _worldTemplateAccessor().TryGetItemTemplate(entry, out itemTemplate);
    }
}
