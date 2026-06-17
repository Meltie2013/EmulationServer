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
// File: src/EmulationServer.Shared/Logging/Formatting/LogMessageFormatter.cs
// Purpose: Contains log message formatter code for the shared infrastructure, logging, timing, and cross-service utility layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Shared.Logging.Formatting;

// Type: LogMessageFormatter
// Purpose: Provides log message formatter behavior for the shared infrastructure, logging, timing, and cross-service utility layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class LogMessageFormatter
{

    // Constant: Defines the default maximum line length constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed default maximum line length value used anywhere this rule or protocol value is needed.
    public const int DefaultMaximumLineLength = 140;

    // Constant: Defines the minimum maximum line length constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed minimum maximum line length value used anywhere this rule or protocol value is needed.
    private const int MinimumMaximumLineLength = 80;

    // Constant: Defines the type label width constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed type label width value used anywhere this rule or protocol value is needed.
    private const int TypeLabelWidth = 5;

    // Method: Format
    // Purpose: Executes the format operation for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - type: Type value supplied by the caller for this operation.
    // - message: Message value supplied by the caller for this operation.
    // - category: Category value supplied by the caller for this operation.
    // - maximumLineLength: Maximum line length value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to LogMessageFormatter so callers do not duplicate validation, protocol, or persistence rules.
    public static string Format(LogType type, string message, string? category = null, int? maximumLineLength = null)
    {
        return string.Join(
            Environment.NewLine,
            FormatLines(type, message, category, maximumLineLength));
    }

    // Method: FormatLines
    // Purpose: Executes the format lines operation for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - type: Type value supplied by the caller for this operation.
    // - message: Message value supplied by the caller for this operation.
    // - category: Category value supplied by the caller for this operation.
    // - maximumLineLength: Maximum line length value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to LogMessageFormatter so callers do not duplicate validation, protocol, or persistence rules.
    public static IReadOnlyList<string> FormatLines(
        LogType type,
        string message,
        string? category = null,
        int? maximumLineLength = null)
    {
        return FormatLines(type, message, category, maximumLineLength, DateTime.UtcNow);
    }

    // Method: FormatLines
    // Purpose: Executes the format lines operation for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - type: Type value supplied by the caller for this operation.
    // - message: Message value supplied by the caller for this operation.
    // - category: Category value supplied by the caller for this operation.
    // - maximumLineLength: Maximum line length value supplied by the caller for this operation.
    // - timestamp: Timestamp value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to LogMessageFormatter so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: BuildPrefix
    // Purpose: Builds or writes build prefix output for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - type: Type value supplied by the caller for this operation.
    // - category: Category value supplied by the caller for this operation.
    // - message: Message value supplied by the caller for this operation.
    // - timestamp: Timestamp value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to LogMessageFormatter so callers do not duplicate validation, protocol, or persistence rules.
    private static string BuildPrefix(LogType type, string? category, string message, DateTime timestamp)
    {
        string timestampText = timestamp.ToString("yyyy-MM-dd HH:mm:ss");
        string typeText = FormatType(type).PadRight(TypeLabelWidth);
        string categoryText = FormatCategory(category, message);

        return string.IsNullOrEmpty(categoryText)
            ? $"{timestampText}  [{typeText}]  "
            : $"{timestampText}  [{typeText}]  [{categoryText}] ";
    }

    // Method: FormatType
    // Purpose: Executes the format type operation for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - type: Type value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to LogMessageFormatter so callers do not duplicate validation, protocol, or persistence rules.
    private static string FormatType(LogType type)
    {
        return type switch
        {
            LogType.NETWORK => "NETWORK",
            LogType.DEBUG => "DEBUG",
            LogType.INFORMATION => "INFORMATION",
            LogType.SYSTEM => "SYSTEM",
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

    // Method: FormatCategory
    // Purpose: Executes the format category operation for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - category: Category value supplied by the caller for this operation.
    // - message: Message value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to LogMessageFormatter so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: NormalizeLineLength
    // Purpose: Converts incoming data into normalize line length form for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - maximumLineLength: Maximum line length value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to LogMessageFormatter so callers do not duplicate validation, protocol, or persistence rules.
    private static int NormalizeLineLength(int? maximumLineLength)
    {
        return Math.Max(MinimumMaximumLineLength, maximumLineLength ?? DefaultMaximumLineLength);
    }

    // Method: NormalizeLineEndings
    // Purpose: Converts incoming data into normalize line endings form for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - message: Message value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to LogMessageFormatter so callers do not duplicate validation, protocol, or persistence rules.
    private static string NormalizeLineEndings(string message)
    {
        return (message ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
    }

    // Method: WrapMessageLine
    // Purpose: Executes the wrap message line operation for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - line: Line value supplied by the caller for this operation.
    // - maximumMessageLength: Maximum message length value supplied by the caller for this operation.
    // Returns: Returns the I enumerable value produced by this operation.
    // Notes: This keeps the operation scoped to LogMessageFormatter so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: FindBreakIndex
    // Purpose: Retrieves find break index data for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // - maximumMessageLength: Maximum message length value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to LogMessageFormatter so callers do not duplicate validation, protocol, or persistence rules.
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
