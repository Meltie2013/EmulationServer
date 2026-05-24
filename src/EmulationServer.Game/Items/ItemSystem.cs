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

using EmulationServer.Game.WorldData;

/**
  * File overview: src/EmulationServer.Game/Items/ItemSystem.cs
  * Documents the ItemSystem source file in the item lookup and item validation support area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Items;

/**
  * Owns the item system behavior for the item lookup and item validation support layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class ItemSystem
{
    /**
      * Holds the private world template accessor state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly Func<WorldTemplateDataStore> _worldTemplateAccessor;

    /**
      * Initializes a new ItemSystem instance with the dependencies required by the item lookup and item validation support workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: worldTemplateAccessor.
      */
    public ItemSystem(Func<WorldTemplateDataStore> worldTemplateAccessor)
    {
        _worldTemplateAccessor = worldTemplateAccessor ?? throw new ArgumentNullException();
    }

    /**
      * Tries to resolve the get item template value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: entry, itemTemplate.
      */
    public bool TryGetItemTemplate(uint entry, out ItemTemplateRecord itemTemplate)
    {
        return _worldTemplateAccessor().TryGetItemTemplate(entry, out itemTemplate);
    }
}
