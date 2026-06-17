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
// File: src/RealmServer/Realms/RealmFlagUtilities.cs
// Purpose: Contains realm flag utilities code for the realm server authentication, realm-list, and account connection layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Globalization;

namespace EmulationServer.RealmServer.Realms;

// Type: RealmFlagUtilities
// Purpose: Provides realm flag utilities behavior for the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class RealmFlagUtilities
{

    public const RealmFlags ConfigurableFlags = RealmFlags.Invalid
        | RealmFlags.Offline
        | RealmFlags.SpecifyBuild
        | RealmFlags.NewPlayers
        | RealmFlags.Recommended;

    // Method: ParseConfigurationValue
    // Purpose: Converts incoming data into parse configuration value form for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the realm flags value produced by this operation.
    // Notes: This keeps the operation scoped to RealmFlagUtilities so callers do not duplicate validation, protocol, or persistence rules.
    public static RealmFlags ParseConfigurationValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return RealmFlags.None;
        }

        RealmFlags flags = RealmFlags.None;

        foreach (string token in SplitFlagTokens(value))
        {
            flags |= ParseToken(token);
        }

        EnsureConfigurationFlagsAreSupported(flags);

        return flags;
    }

    // Method: SanitizeConfiguredFlags
    // Purpose: Executes the sanitize configured flags operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - flags: Flags value supplied by the caller for this operation.
    // Returns: Returns the realm flags value produced by this operation.
    // Notes: This keeps the operation scoped to RealmFlagUtilities so callers do not duplicate validation, protocol, or persistence rules.
    public static RealmFlags SanitizeConfiguredFlags(RealmFlags flags)
    {
        return flags & ConfigurableFlags;
    }

    // Method: EnsureConfigurationFlagsAreSupported
    // Purpose: Validates or evaluates ensure configuration flags are supported rules for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - flags: Flags value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmFlagUtilities so callers do not duplicate validation, protocol, or persistence rules.
    public static void EnsureConfigurationFlagsAreSupported(RealmFlags flags)
    {
        RealmFlags unsupportedFlags = flags & ~ConfigurableFlags;

        if (unsupportedFlags != RealmFlags.None)
        {
            throw new InvalidOperationException(
                $"RealmFlags can only use {FormatAllowedConfigurationFlags()} from configuration. Unsupported value: 0x{((byte)unsupportedFlags):X2}.");
        }
    }

    // Method: FormatAllowedConfigurationFlags
    // Purpose: Executes the format allowed configuration flags operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to RealmFlagUtilities so callers do not duplicate validation, protocol, or persistence rules.
    public static string FormatAllowedConfigurationFlags()
    {
        return "Invalid (0x01), Offline (0x02), SpecifyBuild (0x04), NewPlayers (0x20), Recommended (0x40)";
    }

    // Method: SplitFlagTokens
    // Purpose: Executes the split flag tokens operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the I enumerable value produced by this operation.
    // Notes: This keeps the operation scoped to RealmFlagUtilities so callers do not duplicate validation, protocol, or persistence rules.
    private static IEnumerable<string> SplitFlagTokens(string value)
    {
        return value.Split([';', ',', '|'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    // Method: ParseToken
    // Purpose: Converts incoming data into parse token form for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - token: Token value supplied by the caller for this operation.
    // Returns: Returns the realm flags value produced by this operation.
    // Notes: This keeps the operation scoped to RealmFlagUtilities so callers do not duplicate validation, protocol, or persistence rules.
    private static RealmFlags ParseToken(string token)
    {
        if (TryParseNumericToken(token, out byte numericValue))
        {
            return (RealmFlags)numericValue;
        }

        string normalizedToken = NormalizeToken(token);

        return normalizedToken switch
        {
            "none" => RealmFlags.None,
            "offline" => RealmFlags.Offline,
            "specifybuild" or "specificbuild" => RealmFlags.SpecifyBuild,
            "new" or "newplayers" or "newplayer" => RealmFlags.NewPlayers,
            "recommended" => RealmFlags.Recommended,
            "invalid" => RealmFlags.Invalid,
            "full" => RealmFlags.Full,
            _ => throw new InvalidOperationException($"Unknown realm flag '{token}'. Allowed values: {FormatAllowedConfigurationFlags()}."),
        };
    }

    // Method: TryParseNumericToken
    // Purpose: Attempts to retrieve or parse try parse numeric token data without treating normal misses as failures.
    // Parameters:
    // - token: Token value supplied by the caller for this operation.
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns true when try parse numeric token succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to RealmFlagUtilities so callers do not duplicate validation, protocol, or persistence rules.
    private static bool TryParseNumericToken(string token, out byte value)
    {
        string text = token.Trim();

        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return byte.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }

        return byte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    // Method: NormalizeToken
    // Purpose: Converts incoming data into normalize token form for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - token: Token value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to RealmFlagUtilities so callers do not duplicate validation, protocol, or persistence rules.
    private static string NormalizeToken(string token)
    {
        return token
            .Trim()
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
    }
}
