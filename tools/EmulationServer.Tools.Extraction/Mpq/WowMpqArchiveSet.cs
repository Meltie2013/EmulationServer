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
 * File overview: tools/EmulationServer.Tools.Extraction/Mpq/WowMpqArchiveSet.cs
 * Documents the WowMpqArchiveSet source file in the client data extraction and conversion tooling area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Tools.Extraction.Mpq;

/**
 * Owns the wow mpq archive set behavior for the client data extraction and conversion tooling layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public sealed class WowMpqArchiveSet
{
    /**
     * Holds the private archives state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly IReadOnlyList<WowMpqArchiveEntry> _archives;

    /**
     * Initializes a new WowMpqArchiveSet instance with the dependencies required by the client data extraction and conversion tooling workflow.
     * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
     * Inputs used by this operation: archives.
     */
    private WowMpqArchiveSet(IReadOnlyList<WowMpqArchiveEntry> archives)
    {
        _archives = archives;
    }

    /**
      * Gets or stores the archives value used by WowMpqArchiveSet.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public IReadOnlyList<WowMpqArchiveEntry> Archives => _archives;

    /**
     * Performs the discover operation for the client data extraction and conversion tooling workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: clientRootDirectory, locale.
     */
    public static WowMpqArchiveSet Discover(string clientRootDirectory, string locale)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientRootDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);

        string dataDirectory = Path.Combine(clientRootDirectory, "Data");

        if (!Directory.Exists(dataDirectory))
        {
            throw new DirectoryNotFoundException($"WoW Data directory was not found: {dataDirectory}");
        }

        List<WowMpqArchiveEntry> archives = [];
        AddArchives(archives, dataDirectory, 0);

        string localeDirectory = Path.Combine(dataDirectory, locale);

        if (Directory.Exists(localeDirectory))
        {
            AddArchives(archives, localeDirectory, 10_000);
        }

        if (archives.Count == 0)
        {
            throw new FileNotFoundException($"No MPQ archives were found under {dataDirectory}.");
        }

        return new WowMpqArchiveSet(archives.OrderBy(archive => archive.Priority).ThenBy(archive => archive.Path, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    /**
      * Extracts data from source files and writes the normalized server format.
      * The method is part of WowMpqArchiveSet and keeps this workflow isolated from the caller.
      */
    public AssetCopyReport ExtractKnownFiles(
        Func<string, bool> shouldExtract,
        Func<string, string> getRelativeOutputPath,
        string outputDirectory,
        bool overwrite,
        Action<string>? progressMessage = null)
    {
        ArgumentNullException.ThrowIfNull(shouldExtract);
        ArgumentNullException.ThrowIfNull(getRelativeOutputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        Directory.CreateDirectory(outputDirectory);

        AssetCopyReport report = new();
        Dictionary<string, List<ExtractedArchiveFile>> candidates = new(StringComparer.OrdinalIgnoreCase);

        foreach (WowMpqArchiveEntry archiveEntry in _archives)
        {
            try
            {
                using ManagedMpqArchive archive = ManagedMpqArchive.Open(archiveEntry.Path);

                foreach (string fileName in ReadArchiveListFile(archive))
                {
                    string normalizedName = NormalizeArchivePath(fileName);

                    if (!shouldExtract(normalizedName))
                    {
                        continue;
                    }

                    AddCandidate(candidates, normalizedName, new ExtractedArchiveFile(archiveEntry.Path, archiveEntry.Priority, normalizedName, normalizedName));
                }
            }
            catch (Exception exception) when (IsRecoverableMpqArchiveException(exception))
            {
                AddMessage(report, progressMessage, $"Skipped archive '{archiveEntry.Path}': {exception.Message}");
            }
        }

        foreach ((string normalizedName, List<ExtractedArchiveFile> fileCandidates) in candidates.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            string relativeOutputPath = getRelativeOutputPath(normalizedName);
            string outputPath = Path.Combine(outputDirectory, relativeOutputPath);
            ExtractCandidateFile(fileCandidates, normalizedName, outputPath, overwrite, report, progressMessage);
        }

        report.CandidateFiles = candidates.Count;
        return report;
    }

    /**
      * Extracts data from source files and writes the normalized server format.
      * The method is part of WowMpqArchiveSet and keeps this workflow isolated from the caller.
      */
    public AssetCopyReport ExtractKnownFileNames(
        IEnumerable<string> archiveRelativePaths,
        Func<string, string> getRelativeOutputPath,
        string outputDirectory,
        bool overwrite,
        Action<string>? progressMessage = null)
    {
        ArgumentNullException.ThrowIfNull(archiveRelativePaths);
        ArgumentNullException.ThrowIfNull(getRelativeOutputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        Directory.CreateDirectory(outputDirectory);

        AssetCopyReport report = new();
        string[] requestedFiles = archiveRelativePaths
            .Select(NormalizeArchivePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Dictionary<string, List<ExtractedArchiveFile>> candidates = new(StringComparer.OrdinalIgnoreCase);

        foreach (WowMpqArchiveEntry archiveEntry in _archives)
        {
            try
            {
                using ManagedMpqArchive archive = ManagedMpqArchive.Open(archiveEntry.Path);

                foreach (string requestedFile in requestedFiles)
                {
                    try
                    {
                        if (archive.TryReadFile(requestedFile, out _))
                        {
                            AddCandidate(candidates, requestedFile, new ExtractedArchiveFile(archiveEntry.Path, archiveEntry.Priority, requestedFile, requestedFile));
                        }
                    }
                    catch (Exception exception) when (IsRecoverableMpqFileException(exception))
                    {
                        AddMessage(report, progressMessage, $"Skipped '{requestedFile}' in '{archiveEntry.Path}': {exception.Message}");
                    }
                }
            }
            catch (Exception exception) when (IsRecoverableMpqArchiveException(exception))
            {
                AddMessage(report, progressMessage, $"Skipped archive '{archiveEntry.Path}': {exception.Message}");
            }
        }

        foreach ((string normalizedName, List<ExtractedArchiveFile> fileCandidates) in candidates.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            string relativeOutputPath = getRelativeOutputPath(normalizedName);
            string outputPath = Path.Combine(outputDirectory, relativeOutputPath);
            ExtractCandidateFile(fileCandidates, normalizedName, outputPath, overwrite, report, progressMessage);
        }

        report.CandidateFiles = candidates.Count;

        if (candidates.Count == 0 && requestedFiles.Length > 0)
        {
            AddMessage(report, progressMessage, "No known files were found. Check the client path, locale, and MPQ support for this client build.");
        }

        return report;
    }

    /**
     * Normalizes the archive path for the client data extraction and conversion tooling workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: archivePath.
     */
    public static string NormalizeArchivePath(string archivePath)
    {
        return archivePath.Replace('\\', '/').TrimStart('/');
    }

    /**
      * Adds a new item to the managed collection while preserving internal invariants.
      * The method is part of WowMpqArchiveSet and keeps this workflow isolated from the caller.
      */
    private static void AddCandidate(Dictionary<string, List<ExtractedArchiveFile>> candidates, string normalizedName, ExtractedArchiveFile file)
    {
        if (!candidates.TryGetValue(normalizedName, out List<ExtractedArchiveFile>? fileCandidates))
        {
            fileCandidates = [];
            candidates[normalizedName] = fileCandidates;
        }

        fileCandidates.Add(file);
    }

    /**
      * Extracts data from source files and writes the normalized server format.
      * The method is part of WowMpqArchiveSet and keeps this workflow isolated from the caller.
      */
    private static void ExtractCandidateFile(
        List<ExtractedArchiveFile> candidates,
        string normalizedName,
        string outputPath,
        bool overwrite,
        AssetCopyReport report,
        Action<string>? progressMessage)
    {
        string? parentDirectory = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrWhiteSpace(parentDirectory))
        {
            Directory.CreateDirectory(parentDirectory);
        }

        if (!overwrite && File.Exists(outputPath))
        {
            report.SkippedExisting++;
            AddMessage(report, progressMessage, $"Skipped existing '{normalizedName}'");
            return;
        }

        List<string> failures = [];

        foreach (ExtractedArchiveFile file in candidates.OrderByDescending(candidate => candidate.ArchivePriority))
        {
            try
            {
                using ManagedMpqArchive archive = ManagedMpqArchive.Open(file.ArchivePath);

                if (!archive.TryReadFile(file.OriginalName, out byte[] data))
                {
                    failures.Add($"'{file.ArchivePath}' did not contain the file on second read");
                    continue;
                }

                File.WriteAllBytes(outputPath, data);
                report.ExtractedFiles++;
                AddMessage(report, progressMessage, $"Extracted '{normalizedName}'");
                return;
            }
            catch (Exception exception) when (IsRecoverableMpqArchiveException(exception) || IsRecoverableMpqFileException(exception))
            {
                failures.Add($"'{file.ArchivePath}': {exception.Message}");
            }
        }

        report.FailedFiles++;
        AddMessage(report, progressMessage, $"Failed to extract '{normalizedName}'. Tried {candidates.Count} candidate archive(s): {string.Join("; ", failures)}.");
    }

    /**
      * Adds a new item to the managed collection while preserving internal invariants.
      * The method is part of WowMpqArchiveSet and keeps this workflow isolated from the caller.
      */
    private static void AddMessage(AssetCopyReport report, Action<string>? progressMessage, string message)
    {
        report.Messages.Add(message);
        progressMessage?.Invoke(message);
    }

    /**
      * Adds a new item to the managed collection while preserving internal invariants.
      * The method is part of WowMpqArchiveSet and keeps this workflow isolated from the caller.
      */
    private static void AddArchives(List<WowMpqArchiveEntry> archives, string directory, int basePriority)
    {
        foreach (string path in Directory.EnumerateFiles(directory, "*.MPQ", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(directory, "*.mpq", SearchOption.TopDirectoryOnly)))
        {
            string fileName = Path.GetFileName(path);
            int priority = basePriority + GetArchivePriority(fileName);
            archives.Add(new WowMpqArchiveEntry(path, priority));
        }
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of WowMpqArchiveSet and keeps this workflow isolated from the caller.
      */
    private static int GetArchivePriority(string fileName)
    {
        string lower = fileName.ToLowerInvariant();

        if (lower.StartsWith("patch", StringComparison.Ordinal) || lower.Contains("-patch", StringComparison.Ordinal))
        {
            return 1_000 + ExtractTrailingNumber(lower);
        }

        return lower switch
        {
            "common.mpq" => 10,
            "common-2.mpq" => 20,
            "expansion.mpq" => 30,
            "lichking.mpq" => 40,
            _ when lower.StartsWith("locale-", StringComparison.Ordinal) => 50,
            _ => 100,
        };
    }

    /**
      * Extracts data from source files and writes the normalized server format.
      * The method is part of WowMpqArchiveSet and keeps this workflow isolated from the caller.
      */
    private static int ExtractTrailingNumber(string value)
    {
        int end = value.LastIndexOf('.');

        if (end < 0)
        {
            end = value.Length;
        }

        int start = end - 1;

        while (start >= 0 && char.IsDigit(value[start]))
        {
            start--;
        }

        if (start == end - 1)
        {
            return 1;
        }

        return int.TryParse(value[(start + 1)..end], out int number) ? number : 1;
    }

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of WowMpqArchiveSet and keeps this workflow isolated from the caller.
      */
    private static IReadOnlyCollection<string> ReadArchiveListFile(ManagedMpqArchive archive)
    {
        if (!archive.TryReadFile("(listfile)", out byte[] data) || data.Length == 0)
        {
            return [];
        }

        string listFile = System.Text.Encoding.UTF8.GetString(data);

        return listFile
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
    }

    /**
     * Determines whether recoverable mpq archive exception for the client data extraction and conversion tooling workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: exception.
     */
    private static bool IsRecoverableMpqArchiveException(Exception exception)
    {
        return exception is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException or NotSupportedException;
    }

    /**
     * Determines whether recoverable mpq file exception for the client data extraction and conversion tooling workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: exception.
     */
    private static bool IsRecoverableMpqFileException(Exception exception)
    {
        return exception is IOException or InvalidDataException or ArgumentException or KeyNotFoundException or FileNotFoundException or NotSupportedException;
    }

    /**
      * Represents immutable extracted archive file data passed between parts of the server.
      * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
     * Positional fields carried by this record: ArchivePath, ArchivePriority, OriginalName, NormalizedName.
      */
    private sealed record ExtractedArchiveFile(string ArchivePath, int ArchivePriority, string OriginalName, string NormalizedName);
}
