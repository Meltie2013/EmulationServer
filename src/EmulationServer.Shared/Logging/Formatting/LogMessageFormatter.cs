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

using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/EmulationServer.Shared/Logging/Formatting/LogMessageFormatter.cs
  * Keeps console and file log output in one readable format so every log type is printed consistently.
  * The formatter keeps the prefix short, wraps long messages before the terminal does, and aligns continuation lines under the message text.
  */

namespace EmulationServer.Shared.Logging.Formatting;

/**
  * Builds stable log lines for console and file log writers.
  * The formatter owns timestamp, log-type, category, and long-line wrapping rules so callers only need to supply the message they want to record.
  */
public static class LogMessageFormatter
{
    /**
      * Defines the fallback line length used when the current output target does not report a usable width.
      */
    public const int DefaultMaximumLineLength = 140;

    /**
      * Prevents wrapping from becoming unreadable when a terminal is extremely narrow or reports an invalid width.
      */
    private const int MinimumMaximumLineLength = 80;

    /**
      * Keeps all log type labels visually stable without printing wide enum names such as INFORMATION.
      */
    private const int TypeLabelWidth = 5;

    /**
      * Formats one log message into a single string that may contain wrapped continuation lines.
      */
    public static string Format(LogType type, string message, string? category = null, int? maximumLineLength = null)
    {
        return string.Join(
            Environment.NewLine,
            FormatLines(type, message, category, maximumLineLength));
    }

    /**
      * Formats one log message into physical output lines.
      * Console and file writers use this method so both outputs get the same wrapping and alignment behavior.
      */
    public static IReadOnlyList<string> FormatLines(
        LogType type,
        string message,
        string? category = null,
        int? maximumLineLength = null)
    {
        return FormatLines(type, message, category, maximumLineLength, DateTime.UtcNow);
    }

    /**
      * Formats one log message using a caller-supplied timestamp so multiple output targets can write the same event without drift.
      */
    public static IReadOnlyList<string> FormatLines(
        LogType type,
        string message,
        string? category,
        int? maximumLineLength,
        DateTime timestamp)
    {
        string normalizedMessage = NormalizeLineEndings(message);
        string firstLinePrefix = BuildPrefix(type, category, normalizedMessage, timestamp);
        string continuationPrefix = new(' ', firstLinePrefix.Length);
        int lineLimit = NormalizeLineLength(maximumLineLength);

        List<string> lines = [];
        string[] logicalLines = normalizedMessage.Split('\n');

        for (int logicalLineIndex = 0; logicalLineIndex < logicalLines.Length; logicalLineIndex++)
        {
            string logicalLine = logicalLines[logicalLineIndex];
            string linePrefix = lines.Count == 0 ? firstLinePrefix : continuationPrefix;
            int messageLimit = Math.Max(20, lineLimit - linePrefix.Length);
            bool wroteWrappedLine = false;

            foreach (string wrappedLine in WrapMessageLine(logicalLine, messageLimit))
            {
                lines.Add(linePrefix + wrappedLine);
                linePrefix = continuationPrefix;
                wroteWrappedLine = true;
            }

            if (!wroteWrappedLine)
            {
                lines.Add(linePrefix);
            }
        }

        return lines;
    }

    /**
      * Builds the compact prefix used by normal log lines.
      * The prefix intentionally avoids padded bracket blocks so the useful message text starts earlier in the terminal.
      */
    private static string BuildPrefix(LogType type, string? category, string message, DateTime timestamp)
    {
        string timestampText = timestamp.ToString("yyyy-MM-dd HH:mm:ss");
        string typeText = FormatType(type).PadRight(TypeLabelWidth);
        string categoryText = FormatCategory(category, message);

        return string.IsNullOrEmpty(categoryText)
            ? $"{timestampText}  [{typeText}]  "
            : $"{timestampText}  [{typeText}]  [{categoryText}] ";
    }

    /**
      * Converts verbose enum names into short labels that scan quickly in a busy terminal.
      */
    private static string FormatType(LogType type)
    {
        return type switch
        {
            LogType.NETWORK => "NETWORK",
            LogType.DEBUG => "DEBUG",
            LogType.INFORMATION => "INFORMATION",
            LogType.USER => "USER",
            LogType.SUCCESS => "SUCCESS",
            LogType.WARNING => "WARNING",
            LogType.FAILED => "FAILED",
            LogType.CRITICAL => "CRITICAL",
            LogType.DATABASE => "DATABASE",
            LogType.ALERT => "ALERT",
            LogType.EMERG => "EMERGENCY",
            LogType.FUNC => "FUNCTION",
            LogType.NOTICE => "NOTICE",
            LogType.THREAD => "THREAD",
            LogType.TRACE => "TRACE",
            _ => type.ToString(),
        };
    }

    /**
      * Formats the category prefix unless the caller already included the same category at the start of the message.
      */
    private static string FormatCategory(string? category, string message)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return string.Empty;
        }

        string normalizedCategory = category.Trim();
        return message.StartsWith(normalizedCategory, StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : normalizedCategory;
    }

    /**
      * Normalizes caller-supplied line length values into a safe range for console and file output.
      */
    private static int NormalizeLineLength(int? maximumLineLength)
    {
        return Math.Max(MinimumMaximumLineLength, maximumLineLength ?? DefaultMaximumLineLength);
    }

    /**
      * Converts Windows, Unix, and old Mac line endings into one separator before wrapping starts.
      */
    private static string NormalizeLineEndings(string message)
    {
        return (message ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
    }

    /**
      * Breaks a logical message line into terminal-friendly physical lines.
      * The method prefers commas, semicolons, periods, arrows, and spaces so status summaries wrap at natural boundaries.
      */
    private static IEnumerable<string> WrapMessageLine(string line, int maximumMessageLength)
    {
        if (line.Length == 0)
        {
            yield return string.Empty;
            yield break;
        }

        string remaining = line.TrimEnd();

        while (remaining.Length > maximumMessageLength)
        {
            int breakIndex = FindBreakIndex(remaining, maximumMessageLength);
            string chunk = remaining[..breakIndex].TrimEnd();

            if (chunk.Length == 0)
            {
                breakIndex = Math.Min(maximumMessageLength, remaining.Length);
                chunk = remaining[..breakIndex];
            }

            yield return chunk;
            remaining = remaining[breakIndex..].TrimStart();
        }

        yield return remaining;
    }

    /**
      * Finds the most readable split point inside the requested line width.
      */
    private static int FindBreakIndex(string value, int maximumMessageLength)
    {
        int searchStart = Math.Min(maximumMessageLength, value.Length - 1);
        int minimumPreferredIndex = Math.Max(20, maximumMessageLength / 2);
        string[] preferredBreaks = [", ", "; ", " -> ", ". ", " "];

        foreach (string separator in preferredBreaks)
        {
            int separatorIndex = value.LastIndexOf(separator, searchStart, StringComparison.Ordinal);
            if (separatorIndex >= minimumPreferredIndex)
            {
                return separatorIndex + separator.Length;
            }
        }

        return Math.Min(maximumMessageLength, value.Length);
    }
}
