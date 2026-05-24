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

/**
  * File overview: tools/EmulationServer.Tools.Extraction/Validation/HeightFormulaVerifier.cs
  * Documents the HeightFormulaVerifier source file in the client data extraction and conversion tooling area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Tools.Extraction.Validation;

/**
  * Owns the height formula verifier behavior for the client data extraction and conversion tooling layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public static class HeightFormulaVerifier
{
    /**
      * Verifies that loaded data satisfies the expected format and consistency rules.
      * The method is part of HeightFormulaVerifier and keeps this workflow isolated from the caller.
      */
    public static HeightFormulaVerificationResult Verify(float gridHeight, float gridMaxHeight, int samples)
    {
        if (samples <= 0)
        {
            throw new ArgumentOutOfRangeException(null, samples, "Sample count must be greater than zero.");
        }

        if (gridMaxHeight <= gridHeight)
        {
            throw new ArgumentOutOfRangeException(null, gridMaxHeight, "Grid max height must be greater than grid height.");
        }

        float maxUInt8Error = 0.0f;
        float maxUInt16Error = 0.0f;

        for (int i = 0; i < samples; i++)
        {
            float ratio = samples == 1 ? 0.0f : (float)i / (samples - 1);
            float height = gridHeight + (gridMaxHeight - gridHeight) * ratio;

            byte encoded8 = MapHeightCodec.EncodeUInt8(height, gridHeight, gridMaxHeight);
            float decoded8 = MapHeightCodec.DecodeUInt8(encoded8, gridHeight, gridMaxHeight);
            maxUInt8Error = Math.Max(maxUInt8Error, Math.Abs(height - decoded8));

            ushort encoded16 = MapHeightCodec.EncodeUInt16(height, gridHeight, gridMaxHeight);
            float decoded16 = MapHeightCodec.DecodeUInt16(encoded16, gridHeight, gridMaxHeight);
            maxUInt16Error = Math.Max(maxUInt16Error, Math.Abs(height - decoded16));
        }

        return new HeightFormulaVerificationResult(
            samples,
            gridHeight,
            gridMaxHeight,
            maxUInt8Error,
            MapHeightCodec.MaximumUInt8Error(gridHeight, gridMaxHeight),
            maxUInt16Error,
            MapHeightCodec.MaximumUInt16Error(gridHeight, gridMaxHeight));
    }
}
