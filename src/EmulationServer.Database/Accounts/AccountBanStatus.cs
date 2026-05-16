
namespace EmulationServer.Database.Accounts;

public sealed record AccountBanStatus(bool IsBanned, bool IsPermanent)
{
    public static AccountBanStatus NotBanned { get; } = new(false, false);
}
