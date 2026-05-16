
namespace EmulationServer.RealmServer.Auth;

public enum RealmAuthStatus
{
    Challenge,
    LogonProof,
    Authenticated,
    Closed,
}
