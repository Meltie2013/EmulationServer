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
// File: src/EmulationServer.Game/WorldData/GameObjectDataValidation.cs
// Purpose: Contains game object data validation code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.WorldData;

// Type: GameObjectDataValidation
// Purpose: Provides game object data validation behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class GameObjectDataValidation
{
    // Constant: Defines the maximum classic game object type constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed maximum classic game object type value used anywhere this rule or protocol value is needed.
    public const byte MaximumClassicGameObjectType = 31;
    // Constant: Defines the minimum game object scale constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed minimum game object scale value used anywhere this rule or protocol value is needed.
    private const float MinimumGameObjectScale = 0.0001f;
    // Constant: Defines the maximum game object scale constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed maximum game object scale value used anywhere this rule or protocol value is needed.
    private const float MaximumGameObjectScale = 100.0f;

    // Method: IsLoadableTemplate
    // Purpose: Validates or evaluates is loadable template rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - template: Template value supplied by the caller for this operation.
    // Returns: Returns true when is loadable template succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to GameObjectDataValidation so callers do not duplicate validation, protocol, or persistence rules.
    public static bool IsLoadableTemplate(GameObjectTemplateRecord template)
    {
        ArgumentNullException.ThrowIfNull(template);

        return template.Entry != 0 &&
            template.DisplayId != 0 &&
            template.Type <= MaximumClassicGameObjectType &&
            float.IsFinite(template.Size) &&
            template.Size >= MinimumGameObjectScale &&
            template.Size <= MaximumGameObjectScale;
    }

    // Method: IsLoadableSpawn
    // Purpose: Validates or evaluates is loadable spawn rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - spawn: Spawn value supplied by the caller for this operation.
    // Returns: Returns true when is loadable spawn succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to GameObjectDataValidation so callers do not duplicate validation, protocol, or persistence rules.
    public static bool IsLoadableSpawn(GameObjectSpawnRecord spawn)
    {
        ArgumentNullException.ThrowIfNull(spawn);

        return spawn.Guid != 0 &&
            spawn.Entry != 0 &&
            spawn.ZoneId != 0 &&
            spawn.AreaId != 0 &&
            IsFiniteWorldPosition(spawn.PositionX, spawn.PositionY, spawn.PositionZ) &&
            float.IsFinite(spawn.Orientation) &&
            float.IsFinite(spawn.Rotation0) &&
            float.IsFinite(spawn.Rotation1) &&
            float.IsFinite(spawn.Rotation2) &&
            float.IsFinite(spawn.Rotation3);
    }

    // Method: IsClientVisibleStaticGameObject
    // Purpose: Validates or evaluates is client visible static game object rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - spawn: Spawn value supplied by the caller for this operation.
    // - template: Template value supplied by the caller for this operation.
    // Returns: Returns true when is client visible static game object succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to GameObjectDataValidation so callers do not duplicate validation, protocol, or persistence rules.
    public static bool IsClientVisibleStaticGameObject(GameObjectSpawnRecord spawn, GameObjectTemplateRecord template)
    {
        ArgumentNullException.ThrowIfNull(spawn);
        ArgumentNullException.ThrowIfNull(template);

        if (!IsLoadableSpawn(spawn) || !IsLoadableTemplate(template) || template.Entry != spawn.Entry)
        {
            return false;
        }

        if (template.Type is 11 or 15)
        {

            return false;
        }

        return true;
    }

    // Method: IsFiniteWorldPosition
    // Purpose: Validates or evaluates is finite world position rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - x: X value supplied by the caller for this operation.
    // - y: Y value supplied by the caller for this operation.
    // - z: Z value supplied by the caller for this operation.
    // Returns: Returns true when is finite world position succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to GameObjectDataValidation so callers do not duplicate validation, protocol, or persistence rules.
    private static bool IsFiniteWorldPosition(float x, float y, float z)
    {
        return float.IsFinite(x) && float.IsFinite(y) && float.IsFinite(z);
    }
}
