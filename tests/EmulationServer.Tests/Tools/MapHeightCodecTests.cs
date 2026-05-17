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
    public void UInt8Codec_RoundTripsEndpointsExactly()
    {
        const float minimum = -500.0f;
        const float maximum = 1500.0f;

        byte encodedMinimum = MapHeightCodec.EncodeUInt8(minimum, minimum, maximum);
        byte encodedMaximum = MapHeightCodec.EncodeUInt8(maximum, minimum, maximum);

        Assert.Equal(byte.MinValue, encodedMinimum);
        Assert.Equal(byte.MaxValue, encodedMaximum);
        Assert.Equal(minimum, MapHeightCodec.DecodeUInt8(encodedMinimum, minimum, maximum));
        Assert.Equal(maximum, MapHeightCodec.DecodeUInt8(encodedMaximum, minimum, maximum));
    }

    [Fact]
    public void UInt16Codec_RoundTripsEndpointsExactly()
    {
        const float minimum = -500.0f;
        const float maximum = 1500.0f;

        ushort encodedMinimum = MapHeightCodec.EncodeUInt16(minimum, minimum, maximum);
        ushort encodedMaximum = MapHeightCodec.EncodeUInt16(maximum, minimum, maximum);

        Assert.Equal(ushort.MinValue, encodedMinimum);
        Assert.Equal(ushort.MaxValue, encodedMaximum);
        Assert.Equal(minimum, MapHeightCodec.DecodeUInt16(encodedMinimum, minimum, maximum));
        Assert.Equal(maximum, MapHeightCodec.DecodeUInt16(encodedMaximum, minimum, maximum));
    }

    [Fact]
    public void FormulaVerifier_StaysWithinExpectedQuantizationError()
    {
        HeightFormulaVerificationResult result = HeightFormulaVerifier.Verify(-500.0f, 1500.0f, 10000);

        Assert.True(
            result.IsValid,
            $"UInt8 observed={result.UInt8MaximumObservedError}, allowed={result.UInt8AllowedMaximumError}; " +
            $"UInt16 observed={result.UInt16MaximumObservedError}, allowed={result.UInt16AllowedMaximumError}; " +
            $"tolerance={result.FloatingPointTolerance}.");
    }
}
