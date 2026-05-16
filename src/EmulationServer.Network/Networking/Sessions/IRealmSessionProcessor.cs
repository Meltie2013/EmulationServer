
namespace EmulationServer.Network.Networking.Sessions;

public interface IRealmSessionProcessor
{
    Task ProcessAsync(RealmSessionContext context, CancellationToken cancellationToken);
}
