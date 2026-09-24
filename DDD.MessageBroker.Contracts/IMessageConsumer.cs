namespace DDD.MessageBroker.Contracts;

public interface IMessageConsumer
{
    Task StartAsync(CancellationToken cancellationToken);
}