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

namespace MapStoreViewer.Cli;

/**
  * Small command-line option reader for the standalone viewer.
  */
public sealed class CommandLineOptions
{
    private readonly Dictionary<string, string?> values;

    private CommandLineOptions(Dictionary<string, string?> values)
    {
        this.values = values;
    }

    public static CommandLineOptions Parse(IEnumerable<string> args)
    {
        Dictionary<string, string?> values = new(StringComparer.OrdinalIgnoreCase);
        string[] tokens = args.ToArray();

        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];
            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Unexpected argument '{token}'. Options must start with --.");
            }

            string name = token[2..];
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Empty option name.");
            }

            string? value = null;
            if (i + 1 < tokens.Length && !tokens[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                value = tokens[++i];
            }

            values[name] = value;
        }

        return new CommandLineOptions(values);
    }

    public bool HasFlag(string name)
    {
        return values.ContainsKey(name);
    }

    public string RequireString(string name)
    {
        if (!values.TryGetValue(name, out string? value) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Missing required option --{name}.");
        }

        return value;
    }

    public string GetString(string name, string defaultValue)
    {
        return values.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value) ? value : defaultValue;
    }

    public uint RequireUInt32(string name)
    {
        string value = RequireString(name);
        if (!uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out uint result))
        {
            throw new ArgumentException($"Option --{name} must be an unsigned integer.");
        }

        return result;
    }

    public byte RequireByte(string name)
    {
        string value = RequireString(name);
        if (!byte.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out byte result))
        {
            throw new ArgumentException($"Option --{name} must be a tile coordinate between 0 and 255.");
        }

        return result;
    }

    public int GetInt32(string name, int defaultValue)
    {
        if (!values.TryGetValue(name, out string? value) || string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) || result < 0)
        {
            throw new ArgumentException($"Option --{name} must be a non-negative integer.");
        }

        return result;
    }

    public bool GetBoolean(string name, bool defaultValue)
    {
        if (!values.TryGetValue(name, out string? value))
        {
            return defaultValue;
        }

        if (value is null)
        {
            return true;
        }

        if (bool.TryParse(value, out bool result))
        {
            return result;
        }

        if (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "no", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        throw new ArgumentException($"Option --{name} must be true or false.");
    }
}
