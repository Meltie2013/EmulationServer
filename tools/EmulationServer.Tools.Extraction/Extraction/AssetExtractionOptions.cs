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

using EmulationServer.Tools.Extraction.Client;


/**
 * File overview: tools/EmulationServer.Tools.Extraction/Extraction/AssetExtractionOptions.cs
 * Documents the AssetExtractionOptions source file in the client data extraction and conversion tooling area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Tools.Extraction.Extraction;

/**
 * Owns the asset extraction options behavior for the client data extraction and conversion tooling layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public sealed class AssetExtractionOptions
{
    /**
      * Gets or stores the client root directory value used by AssetExtractionOptions.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string ClientRootDirectory { get; init; } = Directory.GetCurrentDirectory();

    /**
      * Gets or stores the output directory value used by AssetExtractionOptions.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string OutputDirectory { get; init; } = Path.Combine(Directory.GetCurrentDirectory(), "client-data");

    /**
      * Gets or stores the build value used by AssetExtractionOptions.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public ushort Build { get; init; } = ClientBuilds.Wrath335a;

    /**
      * Gets or stores the locale value used by AssetExtractionOptions.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string Locale { get; init; } = "enUS";

    /**
      * Gets or stores the overwrite value used by AssetExtractionOptions.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public bool Overwrite { get; init; } = true;

    /**
      * Gets or stores the progress message value used by AssetExtractionOptions.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public Action<string>? ProgressMessage { get; init; }

    /**
     * Performs the report progress operation for the client data extraction and conversion tooling workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: message.
     */
    public void ReportProgress(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            ProgressMessage?.Invoke(message);
        }
    }

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of AssetExtractionOptions and keeps this workflow isolated from the caller.
      */
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ClientRootDirectory))
        {
            throw new InvalidOperationException("Client root directory is required.");
        }

        if (!Directory.Exists(ClientRootDirectory))
        {
            throw new DirectoryNotFoundException($"Client root directory was not found: {ClientRootDirectory}");
        }

        if (string.IsNullOrWhiteSpace(OutputDirectory))
        {
            throw new InvalidOperationException("Output directory is required.");
        }

        if (!ClientBuilds.IsSupported(Build))
        {
            throw new NotSupportedException($"Client build {Build} is not supported by MapDataTool.");
        }

        if (string.IsNullOrWhiteSpace(Locale))
        {
            throw new InvalidOperationException("Client locale is required. Example: enUS, enGB, deDE.");
        }
    }
}
