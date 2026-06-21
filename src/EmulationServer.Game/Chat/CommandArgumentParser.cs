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
// File: src/EmulationServer.Game/Chat/CommandArgumentParser.cs
// Purpose: Contains command argument parser code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Globalization;

namespace EmulationServer.Game.Commands;

// Type: CommandArgumentParser
// Purpose: Provides command argument parser behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class CommandArgumentParser
{
    // Method: Split
    // Purpose: Executes the split operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - arguments: Arguments value supplied by the caller for this operation.
    // Returns: Returns the string[] value produced by this operation.
    // Notes: This keeps the operation scoped to CommandArgumentParser so callers do not duplicate validation, protocol, or persistence rules.
    public static string[] Split(string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return [];
        }

        List<string> parts = [];
        bool inQuotes = false;
        List<char> current = [];

        foreach (char character in arguments)
        {
            if (character == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(character) && !inQuotes)
            {
                AddPart(parts, current);
                continue;
            }

            current.Add(character);
        }

        AddPart(parts, current);
        return [.. parts];
    }

    // Method: TryParseUnsignedId
    // Purpose: Attempts to retrieve or parse try parse unsigned ID data without treating normal misses as failures.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // - id: Id value supplied by the caller for this operation.
    // Returns: Returns true when try parse unsigned ID succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CommandArgumentParser so callers do not duplicate validation, protocol, or persistence rules.
    public static bool TryParseUnsignedId(string value, out uint id)
    {
        string normalized = RemoveArgumentPrefix(value);
        return uint.TryParse(normalized, NumberStyles.None, CultureInfo.InvariantCulture, out id);
    }

    // Method: TryParseMapId
    // Purpose: Attempts to retrieve or parse try parse map ID data without treating normal misses as failures.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns true when try parse map ID succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CommandArgumentParser so callers do not duplicate validation, protocol, or persistence rules.
    public static bool TryParseMapId(string value, out int mapId)
    {
        mapId = 0;
        string normalized = RemoveArgumentPrefix(value);
        return int.TryParse(normalized, NumberStyles.None, CultureInfo.InvariantCulture, out mapId) && mapId >= 0;
    }

    // Method: TryParseDuration
    // Purpose: Attempts to retrieve or parse try parse duration data without treating normal misses as failures.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // - duration: Duration value supplied by the caller for this operation.
    // Returns: Returns true when try parse duration succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CommandArgumentParser so callers do not duplicate validation, protocol, or persistence rules.
    public static bool TryParseDuration(string value, out TimeSpan duration)
    {
        duration = TimeSpan.Zero;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = RemoveArgumentPrefix(value).Trim().ToLowerInvariant();
        if (normalized == "0")
        {
            return true;
        }

        if (ulong.TryParse(normalized, NumberStyles.None, CultureInfo.InvariantCulture, out ulong seconds))
        {
            return TryCreateDuration(seconds, out duration);
        }

        ulong totalSeconds = 0;
        ulong current = 0;
        bool hasDigits = false;
        bool hasUnit = false;

        foreach (char character in normalized)
        {
            if (char.IsDigit(character))
            {
                hasDigits = true;
                uint digit = (uint)(character - '0');
                if (current > (ulong.MaxValue - digit) / 10)
                {
                    return false;
                }

                current = (current * 10) + digit;
                continue;
            }

            ulong multiplier = character switch
            {
                's' => 1UL,
                'm' => 60UL,
                'h' => 60UL * 60UL,
                'd' => 60UL * 60UL * 24UL,
                'w' => 60UL * 60UL * 24UL * 7UL,
                _ => 0UL
            };

            if (multiplier == 0 || current == 0)
            {
                return false;
            }

            if (current > ulong.MaxValue / multiplier)
            {
                return false;
            }

            ulong segment = current * multiplier;
            if (totalSeconds > ulong.MaxValue - segment)
            {
                return false;
            }

            totalSeconds += segment;
            current = 0;
            hasUnit = true;
        }

        return hasDigits && hasUnit && current == 0 && totalSeconds > 0 && TryCreateDuration(totalSeconds, out duration);
    }

    // Method: FormatDuration
    // Purpose: Executes the format duration operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - duration: Duration value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to CommandArgumentParser so callers do not duplicate validation, protocol, or persistence rules.
    public static string FormatDuration(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return "immediately";
        }

        List<string> parts = [];
        if (duration.Days > 0)
        {
            parts.Add($"{duration.Days} day{(duration.Days == 1 ? string.Empty : "s")}");
        }

        if (duration.Hours > 0)
        {
            parts.Add($"{duration.Hours} hour{(duration.Hours == 1 ? string.Empty : "s")}");
        }

        if (duration.Minutes > 0)
        {
            parts.Add($"{duration.Minutes} minute{(duration.Minutes == 1 ? string.Empty : "s")}");
        }

        if (duration.Seconds > 0 || parts.Count == 0)
        {
            parts.Add($"{duration.Seconds} second{(duration.Seconds == 1 ? string.Empty : "s")}");
        }

        return string.Join(' ', parts);
    }

    // Method: RemoveArgumentPrefix
    // Purpose: Applies remove argument prefix changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to CommandArgumentParser so callers do not duplicate validation, protocol, or persistence rules.
    public static string RemoveArgumentPrefix(string value)
    {
        string normalized = (value ?? string.Empty).Trim();
        return normalized.StartsWith("#", StringComparison.Ordinal) ? normalized[1..].Trim() : normalized;
    }

    // Method: TryCreateDuration
    // Purpose: Executes the try create duration operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - totalSeconds: Total seconds value supplied by the caller for this operation.
    // - duration: Duration value supplied by the caller for this operation.
    // Returns: Returns true when try create duration succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CommandArgumentParser so callers do not duplicate validation, protocol, or persistence rules.
    private static bool TryCreateDuration(ulong totalSeconds, out TimeSpan duration)
    {
        duration = TimeSpan.Zero;
        if (totalSeconds > (ulong)int.MaxValue)
        {
            return false;
        }

        duration = TimeSpan.FromSeconds((int)totalSeconds);
        return true;
    }

    // Method: AddPart
    // Purpose: Applies add part changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - parts: Parts value supplied by the caller for this operation.
    // - current: Current value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to CommandArgumentParser so callers do not duplicate validation, protocol, or persistence rules.
    private static void AddPart(List<string> parts, List<char> current)
    {
        if (current.Count == 0)
        {
            return;
        }

        parts.Add(new string([.. current]));
        current.Clear();
    }
}
