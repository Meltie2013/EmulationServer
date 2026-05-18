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

namespace EmulationServer.Shared.Configuration;

public sealed class IniConfiguration
{
    private readonly Dictionary<string, Dictionary<string, string>> _sections;

    private IniConfiguration(Dictionary<string, Dictionary<string, string>> sections)
    {
        _sections = sections;
    }

    public static IniConfiguration Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ConfigurationException("Configuration path is required.");
        }

        if (!File.Exists(path))
        {
            throw new ConfigurationException($"Configuration file was not found: '{path}'.");
        }

        Dictionary<string, Dictionary<string, string>> sections = new(StringComparer.OrdinalIgnoreCase);

        string currentSection = string.Empty;

        string[] lines;

        try
        {
            lines = File.ReadAllLines(path);
        }
        catch (Exception exception)
        {
            throw new ConfigurationException($"Failed to read configuration file: '{path}'.", exception);
        }

        for (int index = 0; index < lines.Length; index++)
        {
            int lineNumber = index + 1;
            string line = lines[index].Trim();

            if (line.Length == 0)
            {
                continue;
            }

            if (line[0] is ';' or '#')
            {
                continue;
            }

            if (line.StartsWith('['))
            {
                if (!line.EndsWith(']'))
                {
                    throw new ConfigurationException(
                        $"Invalid section declaration at line {lineNumber}: '{line}'.");
                }

                currentSection = line[1..^1].Trim();

                if (currentSection.Length == 0)
                {
                    throw new ConfigurationException(
                        $"Empty section name at line {lineNumber}.");
                }

                if (!sections.ContainsKey(currentSection))
                {
                    sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }

                continue;
            }

            int separatorIndex = line.IndexOf('=');

            if (separatorIndex <= 0)
            {
                throw new ConfigurationException(
                    $"Invalid configuration entry at line {lineNumber}: '{line}'. Expected Key=Value.");
            }

            if (currentSection.Length == 0)
            {
                throw new ConfigurationException(
                    $"Configuration entry outside of a section at line {lineNumber}: '{line}'.");
            }

            string key = line[..separatorIndex].Trim();
            string value = line[(separatorIndex + 1)..].Trim();

            if (key.Length == 0)
            {
                throw new ConfigurationException(
                    $"Empty configuration key at line {lineNumber}.");
            }

            Dictionary<string, string> section = sections[currentSection];

            if (section.ContainsKey(key))
            {
                throw new ConfigurationException(
                    $"Duplicate configuration key '{key}' in section [{currentSection}] at line {lineNumber}.");
            }

            section[key] = value;
        }

        return new IniConfiguration(sections);
    }

    public bool TryGetString(string section, string key, out string value)
    {
        if (_sections.TryGetValue(section, out Dictionary<string, string>? values) &&
            values.TryGetValue(key, out string? foundValue))
        {
            value = foundValue;
            return true;
        }

        value = string.Empty;
        return false;
    }

    public string GetString(string section, string key, string defaultValue)
    {
        return TryGetString(section, key, out string value)
            ? value
            : defaultValue;
    }

    public string GetRequiredString(string section, string key)
    {
        if (!TryGetString(section, key, out string value))
        {
            throw new ConfigurationException($"Missing required configuration value: [{section}] {key}.");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ConfigurationException($"Configuration value cannot be empty: [{section}] {key}.");
        }

        return value;
    }

    public int GetInt(
        string section,
        string key,
        int defaultValue,
        int? minimum = null,
        int? maximum = null)
    {
        if (!TryGetString(section, key, out string value))
        {
            return defaultValue;
        }

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            throw new ConfigurationException(
                $"Invalid integer value for [{section}] {key}: '{value}'.");
        }

        if (minimum.HasValue && parsed < minimum.Value)
        {
            throw new ConfigurationException(
                $"Configuration value [{section}] {key} must be greater than or equal to {minimum.Value}.");
        }

        if (maximum.HasValue && parsed > maximum.Value)
        {
            throw new ConfigurationException(
                $"Configuration value [{section}] {key} must be less than or equal to {maximum.Value}.");
        }

        return parsed;
    }

    public uint GetUInt(
        string section,
        string key,
        uint defaultValue,
        uint? minimum = null,
        uint? maximum = null)
    {
        if (!TryGetString(section, key, out string value))
        {
            return defaultValue;
        }

        if (!uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint parsed))
        {
            throw new ConfigurationException(
                $"Invalid unsigned integer value for [{section}] {key}: '{value}'.");
        }

        if (minimum.HasValue && parsed < minimum.Value)
        {
            throw new ConfigurationException(
                $"Configuration value [{section}] {key} must be greater than or equal to {minimum.Value}.");
        }

        if (maximum.HasValue && parsed > maximum.Value)
        {
            throw new ConfigurationException(
                $"Configuration value [{section}] {key} must be less than or equal to {maximum.Value}.");
        }

        return parsed;
    }

    public bool GetBool(string section, string key, bool defaultValue)
    {
        if (!TryGetString(section, key, out string value))
        {
            return defaultValue;
        }

        return value.ToLowerInvariant() switch
        {
            "true" => true,
            "yes" => true,
            "1" => true,
            "on" => true,

            "false" => false,
            "no" => false,
            "0" => false,
            "off" => false,

            _ => throw new ConfigurationException(
                $"Invalid boolean value for [{section}] {key}: '{value}'. Expected true/false, yes/no, on/off, or 1/0.")
        };
    }

    public TimeSpan GetTimeSpan(string section, string key, TimeSpan defaultValue)
    {
        if (!TryGetString(section, key, out string value))
        {
            return defaultValue;
        }

        if (TryParseDuration(value, out TimeSpan duration))
        {
            return duration;
        }

        if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out TimeSpan parsed))
        {
            return parsed;
        }

        throw new ConfigurationException(
            $"Invalid time span value for [{section}] {key}: '{value}'. Examples: 15s, 5m, 1h, 00:00:15.");
    }

    private static bool TryParseDuration(string value, out TimeSpan duration)
    {
        duration = TimeSpan.Zero;

        string text = value.Trim().ToLowerInvariant();

        if (text.Length == 0)
        {
            return false;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds))
        {
            duration = TimeSpan.FromSeconds(seconds);
            return true;
        }

        (string Suffix, Func<double, TimeSpan> Factory)[] suffixes =
        [
            ("ms", TimeSpan.FromMilliseconds),
            ("s", TimeSpan.FromSeconds),
            ("m", TimeSpan.FromMinutes),
            ("h", TimeSpan.FromHours),
        ];

        foreach ((string suffix, Func<double, TimeSpan> factory) in suffixes)
        {
            if (!text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string numberPart = text[..^suffix.Length];

            if (!double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
            {
                return false;
            }

            duration = factory(number);
            return true;
        }

        return false;
    }
}
