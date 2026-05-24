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

using System.Security.Cryptography;
using System.Text;


/**
 * File overview: tools/EmulationServer.Tools.Extraction/Formats/Vmaps/Conversion/VmapModelName.cs
 * Documents the VmapModelName source file in the client data extraction and conversion tooling area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Tools.Extraction.Formats.Vmaps.Conversion;

/**
  * Represents a normalized WMO model name and its deterministic compact output key.
  * Placements and converted models must use the same key so runtime vmap loading can resolve a placement to its geometry file.
  */
public sealed class VmapModelName
{
    /**
     * Initializes a new VmapModelName instance with the dependencies required by the client data extraction and conversion tooling workflow.
     * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
     * Inputs used by this operation: normalizedPath, key, fileName.
     */
    private VmapModelName(string normalizedPath, string key, string fileName)
    {
        NormalizedPath = normalizedPath;
        Key = key;
        FileName = fileName;
    }

    /**
      * Gets the normalized client path using forward slashes and lower-case comparison semantics.
      */
    public string NormalizedPath { get; }

    /**
      * Gets the deterministic output key shared by model and placement files.
      */
    public string Key { get; }

    /**
      * Gets the original file name portion for human-readable manifests.
      */
    public string FileName { get; }

    /**
      * Creates a model name from a relative path inside the raw vmap source directory.
      */
    public static VmapModelName FromRelativePath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        string normalizedPath = Normalize(relativePath);
        string fileName = Path.GetFileName(normalizedPath);
        string hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(normalizedPath))).ToLowerInvariant()[..16];
        string safeName = Path.GetFileNameWithoutExtension(fileName).Replace(' ', '_');

        return new VmapModelName(normalizedPath, $"{hash}-{safeName}", fileName);
    }

    /**
      * Normalizes client archive paths so references from ADT files and extracted files match.
      */
    public static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return path.Replace('\\', '/')
            .TrimStart('/')
            .ToLowerInvariant();
    }
}
