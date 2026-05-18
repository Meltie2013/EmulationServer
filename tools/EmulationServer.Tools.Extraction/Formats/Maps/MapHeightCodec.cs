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

namespace EmulationServer.Tools.Extraction.Formats.Maps;

public static class MapHeightCodec
{
    public static float SelectUInt8StepStore(float maxDiff)
    {
        ValidateRange(maxDiff);
        return byte.MaxValue / maxDiff;
    }

    public static float SelectUInt16StepStore(float maxDiff)
    {
        ValidateRange(maxDiff);
        return ushort.MaxValue / maxDiff;
    }

    public static byte EncodeUInt8(float height, float gridHeight, float gridMaxHeight)
    {
        float encoded = Encode(height, gridHeight, gridMaxHeight, byte.MaxValue);
        return (byte)Math.Clamp((int)MathF.Round(encoded), byte.MinValue, byte.MaxValue);
    }

    public static ushort EncodeUInt16(float height, float gridHeight, float gridMaxHeight)
    {
        float encoded = Encode(height, gridHeight, gridMaxHeight, ushort.MaxValue);
        return (ushort)Math.Clamp((int)MathF.Round(encoded), ushort.MinValue, ushort.MaxValue);
    }

    public static float DecodeUInt8(byte storedHeight, float gridHeight, float gridMaxHeight)
    {
        return Decode(storedHeight, gridHeight, gridMaxHeight, byte.MaxValue);
    }

    public static float DecodeUInt16(ushort storedHeight, float gridHeight, float gridMaxHeight)
    {
        return Decode(storedHeight, gridHeight, gridMaxHeight, ushort.MaxValue);
    }

    public static float MaximumUInt8Error(float gridHeight, float gridMaxHeight)
    {
        return MaximumQuantizationError(gridHeight, gridMaxHeight, byte.MaxValue);
    }

    public static float MaximumUInt16Error(float gridHeight, float gridMaxHeight)
    {
        return MaximumQuantizationError(gridHeight, gridMaxHeight, ushort.MaxValue);
    }

    private static float Encode(float height, float gridHeight, float gridMaxHeight, int maxStoredValue)
    {
        float range = gridMaxHeight - gridHeight;
        ValidateRange(range);

        float clampedHeight = Math.Clamp(height, gridHeight, gridMaxHeight);
        float step = maxStoredValue / range;
        return (clampedHeight - gridHeight) * step;
    }

    private static float Decode(float storedHeight, float gridHeight, float gridMaxHeight, int maxStoredValue)
    {
        float range = gridMaxHeight - gridHeight;
        ValidateRange(range);

        float multiplier = range / maxStoredValue;
        return storedHeight * multiplier + gridHeight;
    }

    private static float MaximumQuantizationError(float gridHeight, float gridMaxHeight, int maxStoredValue)
    {
        float range = gridMaxHeight - gridHeight;
        ValidateRange(range);
        return range / maxStoredValue / 2.0f;
    }

    private static void ValidateRange(float range)
    {
        if (!float.IsFinite(range) || range <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(range), range, "Height range must be finite and greater than zero.");
        }
    }
}
