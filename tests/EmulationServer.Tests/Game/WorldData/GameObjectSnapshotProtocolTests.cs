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

using EmulationServer.Game.GameObjects;
using EmulationServer.Game.WorldData;

namespace EmulationServer.Tests.Game.WorldData;

public sealed class GameObjectSnapshotProtocolTests
{
    [Fact]
    public void TemplateSnapshot_RoundTripsWhenScriptNameIsEmpty()
    {
        GameObjectTemplateRecord template = new(
            123,
            3,
            456,
            "Test Chest",
            0,
            0,
            1.0f,
            Enumerable.Repeat(0u, GameObjectTemplateRecord.DataFieldCount).ToArray(),
            0,
            0,
            string.Empty);

        string packet = GameObjectSnapshotProtocol.CreateTemplatePacket("snapshot", template);

        Assert.True(GameObjectSnapshotProtocol.TryParseTemplate(packet, out string snapshotId, out GameObjectTemplateRecord parsed));
        Assert.Equal("snapshot", snapshotId);
        Assert.Equal(template.Entry, parsed.Entry);
        Assert.Equal(template.Name, parsed.Name);
        Assert.Equal(string.Empty, parsed.ScriptName);
    }
}
