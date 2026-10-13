namespace DDD.Eventing.Contracts;

public interface IEventReplayService
{
    Task<bool> ReplayAsync(Guid eventId, CancellationToken cancellationToken);
}