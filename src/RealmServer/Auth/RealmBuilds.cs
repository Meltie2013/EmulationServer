
namespace EmulationServer.RealmServer.Auth;

public enum RealmClientExpansion : byte
{
    Classic = 0,
    TheBurningCrusade = 1,
    WrathOfTheLichKing = 2,
    Cataclysm = 3,
}

public static class RealmBuilds
{
    public const ushort Vanilla1121 = 5875;
    public const ushort Vanilla1122 = 6005;
    public const ushort Vanilla1123 = 6141;

    public const ushort TheBurningCrusade243 = 8606;

    public const ushort Wrath335a = 12340;

    public const ushort Cataclysm434 = 15595;

    private static readonly HashSet<ushort> SupportedBuilds =
    [
        Vanilla1121,
        Vanilla1122,
        Vanilla1123,
        TheBurningCrusade243,
        Wrath335a,
        Cataclysm434,
    ];

    public static bool IsSupported(ushort build)
    {
        return SupportedBuilds.Contains(build);
    }

    public static bool IsVanilla(ushort build)
    {
        return build is Vanilla1121 or Vanilla1122 or Vanilla1123;
    }

    public static bool IsTheBurningCrusade(ushort build)
    {
        return build is TheBurningCrusade243;
    }

    public static bool IsWrath(ushort build)
    {
        return build is Wrath335a;
    }

    public static bool IsCataclysm(ushort build)
    {
        return build is Cataclysm434;
    }

    public static bool UsesModernProofResponse(ushort build)
    {
        return !IsVanilla(build);
    }

    public static bool UsesModernRealmList(ushort build)
    {
        return !IsVanilla(build);
    }

    public static RealmClientExpansion GetExpansion(ushort build)
    {
        return build switch
        {
            Vanilla1121 or Vanilla1122 or Vanilla1123 => RealmClientExpansion.Classic,
            TheBurningCrusade243 => RealmClientExpansion.TheBurningCrusade,
            Wrath335a => RealmClientExpansion.WrathOfTheLichKing,
            Cataclysm434 => RealmClientExpansion.Cataclysm,
            _ => RealmClientExpansion.Classic,
        };
    }

    public static string GetDisplayName(ushort build)
    {
        return build switch
        {
            Vanilla1121 => "1.12.1",
            Vanilla1122 => "1.12.2",
            Vanilla1123 => "1.12.3",
            TheBurningCrusade243 => "2.4.3",
            Wrath335a => "3.3.5a",
            Cataclysm434 => "4.3.4",
            _ => $"Unknown ({build})",
        };
    }
}
