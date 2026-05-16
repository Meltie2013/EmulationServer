
namespace EmulationServer.Database.Accounts;

public sealed record AccountLogonRecord(
    uint Id,
    string Username,
    string ShaPassHash,
    byte GmLevel,
    bool Locked,
    string LastIp,
    string? Verifier,
    string? Salt);
