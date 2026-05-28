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

using System.Globalization;

namespace EmulationServer.RealmServer.Realms;

/**
  * Provides parsing and validation helpers for realm-list flags configured by administrators.
  */
public static class RealmFlagUtilities
{
    /**
      * Flags accepted from realm configuration.
      * Keeps only these config flags and masks the remaining protocol bits out.
      */
    public const RealmFlags ConfigurableFlags = RealmFlags.Invalid
        | RealmFlags.Offline
        | RealmFlags.SpecifyBuild
        | RealmFlags.NewPlayers
        | RealmFlags.Recommended;

    /**
      * Parses RealmFlags from decimal, hexadecimal, or symbolic text.
      * Examples: 0, 0x20, NewPlayers, Recommended, NewPlayers|Recommended.
      */
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

    /**
      * Removes unsupported config flags and preserves only supported configured bits.
      */
    public static RealmFlags SanitizeConfiguredFlags(RealmFlags flags)
    {
        return flags & ConfigurableFlags;
    }

    /**
      * Throws when unsupported protocol-only flags are supplied through configuration.
      */
    public static void EnsureConfigurationFlagsAreSupported(RealmFlags flags)
    {
        RealmFlags unsupportedFlags = flags & ~ConfigurableFlags;

        if (unsupportedFlags != RealmFlags.None)
        {
            throw new InvalidOperationException(
                $"RealmFlags can only use {FormatAllowedConfigurationFlags()} from configuration. Unsupported value: 0x{((byte)unsupportedFlags):X2}.");
        }
    }

    /**
      * Returns a user-readable list of safe configuration flags.
      */
    public static string FormatAllowedConfigurationFlags()
    {
        return "Invalid (0x01), Offline (0x02), SpecifyBuild (0x04), NewPlayers (0x20), Recommended (0x40)";
    }

    private static IEnumerable<string> SplitFlagTokens(string value)
    {
        return value.Split([';', ',', '|'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

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

    private static bool TryParseNumericToken(string token, out byte value)
    {
        string text = token.Trim();

        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return byte.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }

        return byte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

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
