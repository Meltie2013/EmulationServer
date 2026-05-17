
using EmulationServer.Tools.Extraction.Formats.Maps;
using EmulationServer.Tools.Extraction.Validation;

namespace EmulationServer.Tests.Tools;

public sealed class MapHeightCodecTests
{
    [Fact]
    public void SelectUInt8StepStore_MatchesMangosFormula()
    {
        Assert.Equal(127.5f, MapHeightCodec.SelectUInt8StepStore(2.0f));
    }

    [Fact]
    public void SelectUInt16StepStore_MatchesMangosFormula()
    {
        Assert.Equal(32.7675f, MapHeightCodec.SelectUInt16StepStore(2000.0f));
    }

    [Fact]
    public void FormulaVerifier_StaysWithinExpectedQuantizationError()
    {
        HeightFormulaVerificationResult result = HeightFormulaVerifier.Verify(-500.0f, 1500.0f, 10000);
        Assert.True(result.IsValid);
    }
}
