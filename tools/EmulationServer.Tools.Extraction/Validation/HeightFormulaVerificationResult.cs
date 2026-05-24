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

/**
  * File overview: tools/EmulationServer.Tools.Extraction/Validation/HeightFormulaVerificationResult.cs
  * Documents the HeightFormulaVerificationResult source file in the client data extraction and conversion tooling area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Tools.Extraction.Validation;

/**
  * Represents immutable height formula verification result data passed between parts of the server.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  * Positional fields carried by this record: Samples, GridHeight, GridMaxHeight, UInt8MaximumObservedError, UInt8ExpectedMaximumError, UInt16MaximumObservedError, UInt16ExpectedMaximumError.
  */
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
    /**
      * Gets or stores the floating point tolerance value used by HeightFormulaVerificationResult.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public float FloatingPointTolerance => Math.Max(0.00025f, Math.Abs(GridMaxHeight - GridHeight) * 0.0000001f);

    /**
      * Gets or stores the uint8 allowed maximum error value used by HeightFormulaVerificationResult.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public float UInt8AllowedMaximumError => UInt8ExpectedMaximumError + FloatingPointTolerance;

    /**
      * Gets or stores the uint16 allowed maximum error value used by HeightFormulaVerificationResult.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public float UInt16AllowedMaximumError => UInt16ExpectedMaximumError + FloatingPointTolerance;

    /**
      * Gets or stores the is valid value used by HeightFormulaVerificationResult.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public bool IsValid =>
        UInt8MaximumObservedError <= UInt8AllowedMaximumError &&
        UInt16MaximumObservedError <= UInt16AllowedMaximumError;
}
