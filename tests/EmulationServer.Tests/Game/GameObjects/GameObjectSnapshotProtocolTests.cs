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

using System.Globalization;
using EmulationServer.Game.GameObjects;
using EmulationServer.Game.WorldData;

namespace EmulationServer.Tests.Game.GameObjects;

public sealed class GameObjectSnapshotProtocolTests
{
    [Fact]
    public void CreateTemplatePacket_PreservesEmptyScriptNameAsNonEmptyToken()
    {
        GameObjectTemplateRecord source = CreateTemplate(300132, string.Empty);

        string packet = GameObjectSnapshotProtocol.CreateTemplatePacket("snapshot", source);
        string[] parts = packet.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(13, parts.Length);
        Assert.True(GameObjectSnapshotProtocol.TryParseTemplate(packet, out string snapshotId, out GameObjectTemplateRecord parsed));
        Assert.Equal("snapshot", snapshotId);
        Assert.Equal(source.Entry, parsed.Entry);
        Assert.Equal(source.Name, parsed.Name);
        Assert.Equal(string.Empty, parsed.ScriptName);
    }

    [Fact]
    public void TryParseTemplate_AcceptsLegacyPacketMissingTrailingScriptName()
    {
        GameObjectTemplateRecord source = CreateTemplate(300027, string.Empty);
        string packet = GameObjectSnapshotProtocol.CreateTemplatePacket("snapshot", source);
        string legacyPacket = string.Join(' ', packet.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Take(12));

        Assert.True(GameObjectSnapshotProtocol.TryParseTemplate(legacyPacket, out string snapshotId, out GameObjectTemplateRecord parsed));
        Assert.Equal("snapshot", snapshotId);
        Assert.Equal(source.Entry, parsed.Entry);
        Assert.Equal(source.Name, parsed.Name);
        Assert.Equal(string.Empty, parsed.ScriptName);
    }

    private static GameObjectTemplateRecord CreateTemplate(uint entry, string scriptName)
    {
        uint[] dataFields = Enumerable.Repeat(0u, GameObjectTemplateRecord.DataFieldCount).ToArray();
        dataFields[0] = 783;
        dataFields[1] = 10;

        return new GameObjectTemplateRecord(
            entry,
            8,
            0,
            string.Create(CultureInfo.InvariantCulture, $"TEST GameObject {entry}"),
            0,
            0,
            1.0f,
            dataFields,
            0,
            0,
            scriptName);
    }
}
