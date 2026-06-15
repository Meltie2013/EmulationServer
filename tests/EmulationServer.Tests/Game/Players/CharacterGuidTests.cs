using EmulationServer.Game.Players;

namespace EmulationServer.Tests.Game.Players;

public sealed class CharacterGuidTests
{
    [Fact]
    public void ToGameObjectGuid_ShouldIncludeHighGuidEntryAndSpawnGuid()
    {
        ulong guid = CharacterGuid.ToGameObjectGuid(0x345678, 0x123456);

        Assert.Equal(0xF110UL, guid >> 48);
        Assert.Equal(0x123456UL, (guid >> 24) & 0xFFFFFFUL);
        Assert.Equal(0x345678UL, guid & 0xFFFFFFUL);
    }

    [Fact]
    public void ToGameObjectGuid_ShouldReturnZeroForMissingSpawnOrEntry()
    {
        Assert.Equal(0UL, CharacterGuid.ToGameObjectGuid(0, 1));
        Assert.Equal(0UL, CharacterGuid.ToGameObjectGuid(1, 0));
    }
}
