
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
