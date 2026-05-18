namespace EmulationServer.Game.Maps.Runtime;

public sealed record MapServiceControlResult(
    string OwnerServerName,
    MapServiceKind Kind,
    int MapId,
    long InstanceId,
    MapServiceControlResultCode ResultCode,
    MapServiceState State,
    string Message)
{
    public static MapServiceControlResult FromSnapshot(
        MapServiceSnapshot snapshot,
        MapServiceControlResultCode resultCode,
        string message)
    {
        return new MapServiceControlResult(
            snapshot.OwnerServerName,
            snapshot.Kind,
            snapshot.MapId,
            snapshot.InstanceId,
            resultCode,
            snapshot.State,
            message);
    }
}
