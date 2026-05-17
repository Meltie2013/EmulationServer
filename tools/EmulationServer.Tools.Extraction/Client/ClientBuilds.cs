
using System.Diagnostics.CodeAnalysis;

namespace EmulationServer.Tools.Extraction.Client;

public static class ClientBuilds
{
    public const ushort Classic1121 = 5875;
    public const ushort Classic1122 = 6005;
    public const ushort Classic1123 = 6141;
    public const ushort TheBurningCrusade243 = 8606;
    public const ushort Wrath335a = 12340;

    private static readonly IReadOnlyDictionary<ushort, ClientBuildInfo> BuildsByNumber =
        new Dictionary<ushort, ClientBuildInfo>
        {
            [Classic1121] = new(Classic1121, "1.12.1", SupportedClientExpansion.Classic, "Mangos Zero"),
            [Classic1122] = new(Classic1122, "1.12.2", SupportedClientExpansion.Classic, "Mangos Zero"),
            [Classic1123] = new(Classic1123, "1.12.3", SupportedClientExpansion.Classic, "Mangos Zero"),
            [TheBurningCrusade243] = new(TheBurningCrusade243, "2.4.3", SupportedClientExpansion.TheBurningCrusade, "Mangos One"),
            [Wrath335a] = new(Wrath335a, "3.3.5a", SupportedClientExpansion.WrathOfTheLichKing, "Mangos Two"),
        };

    public static IReadOnlyCollection<ClientBuildInfo> All => BuildsByNumber.Values.ToArray();

    public static bool IsSupported(ushort build)
    {
        return BuildsByNumber.ContainsKey(build);
    }

    public static bool TryGet(ushort build, [NotNullWhen(true)] out ClientBuildInfo? buildInfo)
    {
        return BuildsByNumber.TryGetValue(build, out buildInfo);
    }

    public static ClientBuildInfo Require(ushort build)
    {
        if (TryGet(build, out ClientBuildInfo? buildInfo))
        {
            return buildInfo;
        }

        throw new NotSupportedException($"Client build {build} is not supported by these extraction tools.");
    }
}
