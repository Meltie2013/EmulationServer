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

namespace EmulationServer.Tools.Extraction.Validation;

public sealed record HeightFormulaVerificationResult(
    int Samples,
    float GridHeight,
    float GridMaxHeight,
    float UInt8MaximumObservedError,
    float UInt8ExpectedMaximumError,
    float UInt16MaximumObservedError,
    float UInt16ExpectedMaximumError)
{
    // The formula gives the ideal half-step quantization error. The implementation
    // intentionally uses float math because the generated map data is float-based,
    // so allow a small tolerance for single-precision rounding during encode/decode.
    public float FloatingPointTolerance => Math.Max(0.00025f, Math.Abs(GridMaxHeight - GridHeight) * 0.0000001f);

    public float UInt8AllowedMaximumError => UInt8ExpectedMaximumError + FloatingPointTolerance;

    public float UInt16AllowedMaximumError => UInt16ExpectedMaximumError + FloatingPointTolerance;

    public bool IsValid =>
        UInt8MaximumObservedError <= UInt8AllowedMaximumError &&
        UInt16MaximumObservedError <= UInt16AllowedMaximumError;
}
