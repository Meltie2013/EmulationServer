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
 * Documents the MapHeightCodecTests source file in the automated test coverage for server behavior and data helpers area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Tests.Tools;

/**
 * Owns the map height codec tests behavior for the automated test coverage for server behavior and data helpers layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public sealed class MapHeightCodecTests
{
    /**
     * Performs the select u int 8 step store matches mangos formula operation for the automated test coverage for server behavior and data helpers workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     */
    [Fact]
    public void SelectUInt8StepStore_MatchesMangosFormula()
    {
        Assert.Equal(127.5f, MapHeightCodec.SelectUInt8StepStore(2.0f));
    }

    /**
     * Performs the select u int 16 step store matches mangos formula operation for the automated test coverage for server behavior and data helpers workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     */
    [Fact]
    public void SelectUInt16StepStore_MatchesMangosFormula()
    {
        Assert.Equal(32.7675f, MapHeightCodec.SelectUInt16StepStore(2000.0f));
    }

    /**
     * Performs the u int 8 codec round trips endpoints exactly operation for the automated test coverage for server behavior and data helpers workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     */
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

    /**
     * Performs the u int 16 codec round trips endpoints exactly operation for the automated test coverage for server behavior and data helpers workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     */
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

    /**
     * Performs the formula verifier stays within expected quantization error operation for the automated test coverage for server behavior and data helpers workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     */
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
