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
 * File overview: tools/EmulationServer.Tools.Extraction/Formats/Maps/MapHeightCodec.cs
 * Documents the MapHeightCodec source file in the client data extraction and conversion tooling area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Tools.Extraction.Formats.Maps;

/**
 * Owns the map height codec behavior for the client data extraction and conversion tooling layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public static class MapHeightCodec
{
    /**
     * Performs the select u int 8 step store operation for the client data extraction and conversion tooling workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: maxDiff.
     */
    public static float SelectUInt8StepStore(float maxDiff)
    {
        ValidateRange(maxDiff);
        return byte.MaxValue / maxDiff;
    }

    /**
     * Performs the select u int 16 step store operation for the client data extraction and conversion tooling workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: maxDiff.
     */
    public static float SelectUInt16StepStore(float maxDiff)
    {
        ValidateRange(maxDiff);
        return ushort.MaxValue / maxDiff;
    }

    /**
     * Performs the encode u int 8 operation for the client data extraction and conversion tooling workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: height, gridHeight, gridMaxHeight.
     */
    public static byte EncodeUInt8(float height, float gridHeight, float gridMaxHeight)
    {
        float encoded = Encode(height, gridHeight, gridMaxHeight, byte.MaxValue);
        return (byte)Math.Clamp((int)MathF.Round(encoded), byte.MinValue, byte.MaxValue);
    }

    /**
     * Performs the encode u int 16 operation for the client data extraction and conversion tooling workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: height, gridHeight, gridMaxHeight.
     */
    public static ushort EncodeUInt16(float height, float gridHeight, float gridMaxHeight)
    {
        float encoded = Encode(height, gridHeight, gridMaxHeight, ushort.MaxValue);
        return (ushort)Math.Clamp((int)MathF.Round(encoded), ushort.MinValue, ushort.MaxValue);
    }

    /**
     * Performs the decode u int 8 operation for the client data extraction and conversion tooling workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: storedHeight, gridHeight, gridMaxHeight.
     */
    public static float DecodeUInt8(byte storedHeight, float gridHeight, float gridMaxHeight)
    {
        return Decode(storedHeight, gridHeight, gridMaxHeight, byte.MaxValue);
    }

    /**
     * Performs the decode u int 16 operation for the client data extraction and conversion tooling workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: storedHeight, gridHeight, gridMaxHeight.
     */
    public static float DecodeUInt16(ushort storedHeight, float gridHeight, float gridMaxHeight)
    {
        return Decode(storedHeight, gridHeight, gridMaxHeight, ushort.MaxValue);
    }

    /**
     * Performs the maximum u int 8 error operation for the client data extraction and conversion tooling workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: gridHeight, gridMaxHeight.
     */
    public static float MaximumUInt8Error(float gridHeight, float gridMaxHeight)
    {
        return MaximumQuantizationError(gridHeight, gridMaxHeight, byte.MaxValue);
    }

    /**
     * Performs the maximum u int 16 error operation for the client data extraction and conversion tooling workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: gridHeight, gridMaxHeight.
     */
    public static float MaximumUInt16Error(float gridHeight, float gridMaxHeight)
    {
        return MaximumQuantizationError(gridHeight, gridMaxHeight, ushort.MaxValue);
    }

    /**
     * Performs the encode operation for the client data extraction and conversion tooling workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: height, gridHeight, gridMaxHeight, maxStoredValue.
     */
    private static float Encode(float height, float gridHeight, float gridMaxHeight, int maxStoredValue)
    {
        float range = gridMaxHeight - gridHeight;
        ValidateRange(range);

        float clampedHeight = Math.Clamp(height, gridHeight, gridMaxHeight);
        float step = maxStoredValue / range;
        return (clampedHeight - gridHeight) * step;
    }

    /**
     * Performs the decode operation for the client data extraction and conversion tooling workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: storedHeight, gridHeight, gridMaxHeight, maxStoredValue.
     */
    private static float Decode(float storedHeight, float gridHeight, float gridMaxHeight, int maxStoredValue)
    {
        float range = gridMaxHeight - gridHeight;
        ValidateRange(range);

        float multiplier = range / maxStoredValue;
        return storedHeight * multiplier + gridHeight;
    }

    /**
     * Performs the maximum quantization error operation for the client data extraction and conversion tooling workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: gridHeight, gridMaxHeight, maxStoredValue.
     */
    private static float MaximumQuantizationError(float gridHeight, float gridMaxHeight, int maxStoredValue)
    {
        float range = gridMaxHeight - gridHeight;
        ValidateRange(range);
        return range / maxStoredValue / 2.0f;
    }

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of MapHeightCodec and keeps this workflow isolated from the caller.
      */
    private static void ValidateRange(float range)
    {
        if (!float.IsFinite(range) || range <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(range), range, "Height range must be finite and greater than zero.");
        }
    }
}
