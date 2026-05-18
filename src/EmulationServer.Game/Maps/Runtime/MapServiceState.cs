namespace EmulationServer.Game.Maps.Runtime;

public enum MapServiceState
{
    Offline = 0,
    Starting = 1,
    Online = 2,
    RestartRequested = 3,
    DrainingPlayers = 4,
    SavingPlayers = 5,
    UnloadingObjects = 6,
    ReloadingData = 7,
    RespawningObjects = 8,
    Stopping = 9,
    Faulted = 10,
}
