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

using EmulationServer.Tools.Extraction.Formats.Maps;
using EmulationServer.Tools.Extraction.Validation;

/**
  * File overview: tests/EmulationServer.Tests/Tools/MapHeightCodecTests.cs
  * This file belongs to the project runtime logic and supporting data models portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Tests.Tools;

/**
  * Represents the map height codec tests component in the project runtime logic and supporting data models area.
  * It documents expected behavior with automated assertions so regressions are easier to detect.
  */
public sealed class MapHeightCodecTests
{
    [Fact]
    /**
      * Performs the select uint8 step store matches mangos formula operation for MapHeightCodecTests.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    public void SelectUInt8StepStore_MatchesMangosFormula()
    {
        Assert.Equal(127.5f, MapHeightCodec.SelectUInt8StepStore(2.0f));
    }

    [Fact]
    /**
      * Performs the select uint16 step store matches mangos formula operation for MapHeightCodecTests.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    public void SelectUInt16StepStore_MatchesMangosFormula()
    {
        Assert.Equal(32.7675f, MapHeightCodec.SelectUInt16StepStore(2000.0f));
    }

    [Fact]
    /**
      * Performs the uint8 codec round trips endpoints exactly operation for MapHeightCodecTests.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
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
    /**
      * Performs the uint16 codec round trips endpoints exactly operation for MapHeightCodecTests.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
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
    /**
      * Performs the formula verifier stays within expected quantization error operation for MapHeightCodecTests.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
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
