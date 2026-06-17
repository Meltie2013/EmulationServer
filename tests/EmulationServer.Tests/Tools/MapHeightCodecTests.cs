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
// File: tests/EmulationServer.Tests/Tools/MapHeightCodecTests.cs
// Purpose: Contains map height codec tests code for the automated test and verification layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Tools.Extraction.Formats.Maps;
using EmulationServer.Tools.Extraction.Validation;

namespace EmulationServer.Tests.Tools;

// Type: MapHeightCodecTests
// Purpose: Provides map height codec tests behavior for the automated test and verification layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class MapHeightCodecTests
{

    [Fact]
    // Method: SelectUInt8StepStore_MatchesMangosFormula
    // Purpose: Retrieves select U int8 step store matches mangos formula data for the automated test and verification layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapHeightCodecTests so callers do not duplicate validation, protocol, or persistence rules.
    public void SelectUInt8StepStore_MatchesMangosFormula()
    {
        Assert.Equal(127.5f, MapHeightCodec.SelectUInt8StepStore(2.0f));
    }

    [Fact]
    // Method: SelectUInt16StepStore_MatchesMangosFormula
    // Purpose: Retrieves select U int16 step store matches mangos formula data for the automated test and verification layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapHeightCodecTests so callers do not duplicate validation, protocol, or persistence rules.
    public void SelectUInt16StepStore_MatchesMangosFormula()
    {
        Assert.Equal(32.7675f, MapHeightCodec.SelectUInt16StepStore(2000.0f));
    }

    [Fact]
    // Method: UInt8Codec_RoundTripsEndpointsExactly
    // Purpose: Executes the U int8 codec round trips endpoints exactly operation for the automated test and verification layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapHeightCodecTests so callers do not duplicate validation, protocol, or persistence rules.
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
    // Method: UInt16Codec_RoundTripsEndpointsExactly
    // Purpose: Executes the U int16 codec round trips endpoints exactly operation for the automated test and verification layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapHeightCodecTests so callers do not duplicate validation, protocol, or persistence rules.
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
    // Method: FormulaVerifier_StaysWithinExpectedQuantizationError
    // Purpose: Executes the formula verifier stays within expected quantization error operation for the automated test and verification layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapHeightCodecTests so callers do not duplicate validation, protocol, or persistence rules.
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
