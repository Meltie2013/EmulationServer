
using EmulationServer.Tools.Extraction.Client;

namespace EmulationServer.Tests.Tools;

public sealed class ClientBuildsTests
{
    [Theory]
    [InlineData(5875)]
    [InlineData(6005)]
    [InlineData(6141)]
    [InlineData(8606)]
    [InlineData(12340)]
    public void IsSupported_ReturnsTrue_ForExpectedExtractorBuilds(ushort build)
    {
        Assert.True(ClientBuilds.IsSupported(build));
    }

    [Fact]
    public void Require_Throws_ForUnsupportedBuild()
    {
        Assert.Throws<NotSupportedException>(() => ClientBuilds.Require(15595));
    }
}
