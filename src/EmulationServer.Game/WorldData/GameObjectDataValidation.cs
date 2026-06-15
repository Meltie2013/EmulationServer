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

namespace EmulationServer.Game.WorldData;

/**
  * Centralizes the hard gameobject data gates used by the world cache, internal snapshots, map runtime, and client visibility.
  * A zero display id means the client cannot render the template. A zero zoneId or areaId means the spawn has not been resolved to
  * a real world area yet, so the row is treated as unavailable instead of being pushed into map/player visibility.
  */
public static class GameObjectDataValidation
{
    public const byte MaximumClassicGameObjectType = 31;
    private const float MinimumGameObjectScale = 0.0001f;
    private const float MaximumGameObjectScale = 100.0f;

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
            // Transport-style gameobjects need path/progress handling before they are safe to expose to the client.
            return false;
        }

        return true;
    }

    private static bool IsFiniteWorldPosition(float x, float y, float z)
    {
        return float.IsFinite(x) && float.IsFinite(y) && float.IsFinite(z);
    }
}
