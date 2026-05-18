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

namespace EmulationServer.Tools.Extraction.Extraction;

public sealed class AssetExtractionOptions
{
    public string ClientRootDirectory { get; init; } = Directory.GetCurrentDirectory();

    public string OutputDirectory { get; init; } = Path.Combine(Directory.GetCurrentDirectory(), "client-data");

    public ushort Build { get; init; } = ClientBuilds.Wrath335a;

    public string Locale { get; init; } = "enUS";

    public bool Overwrite { get; init; } = true;

    public Action<string>? ProgressMessage { get; init; }

    public void ReportProgress(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            ProgressMessage?.Invoke(message);
        }
    }

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
