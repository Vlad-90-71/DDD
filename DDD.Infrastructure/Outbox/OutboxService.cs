using DDD.Eventing.Contracts;
using Microsoft.EntityFrameworkCore;

namespace DDD.Infrastructure.Outbox;

public interface IOutboxService
{
    Task RetryAsync(long id, CancellationToken cancellationToken);
    Task RedeliverAsync(long id, CancellationToken cancellationToken);
}
public class OutboxService(
    AppDbContext context,
    IOutboxMessageSerializer serializer,
    IDomainEventDispatcher dispatcher) : IOutboxService
{
    public async Task RetryAsync(long id, CancellationToken cancellationToken)
    {
        var message = await context.OutboxMessages
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken) 
                ?? throw new KeyNotFoundException($"Outbox-сообщение с Id '{id}' не найдено.");

        message.Retry();

        await context.SaveChangesAsync(cancellationToken);
    }
    public async Task RedeliverAsync(long id, CancellationToken cancellationToken)
    {
        var message = await context.OutboxMessages
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Outbox-сообщение с Id '{id}' не найдено.");

        await dispatcher.DispatchAsync(serializer.Deserialize(message), cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }
}