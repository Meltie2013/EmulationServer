
namespace EmulationServer.RealmServer.Auth;

public enum RealmAuthResult : byte
{
    Success = 0x00,
    Failed = 0x01,
    Banned = 0x03,
    UnknownAccount = 0x04,
    AlreadyOnline = 0x06,
    NoTime = 0x07,
    DatabaseBusy = 0x08,
    VersionInvalid = 0x09,
    VersionUpdate = 0x0A,
    InvalidServer = 0x0B,
    Suspended = 0x0C,
    NoAccess = 0x0D,
    LockedEnforced = 0x10,
}
