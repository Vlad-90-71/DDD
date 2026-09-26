using DDD.MessageBroker.Contracts;
using Microsoft.EntityFrameworkCore;

namespace DDD.Infrastructure.Outbox;

public interface IOutboxService
{
    Task RetryAsync(long id, CancellationToken cancellationToken);
    Task RedeliverAsync(long id, CancellationToken cancellationToken);
}

public sealed class OutboxService(AppDbContext context, IMessagePublisher publisher) : IOutboxService
{
    public async Task RetryAsync(long id, CancellationToken cancellationToken)
    {
        var message = await context.OutboxMessages
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Outbox-сообщение с Id '{id}' не найдено.");

        message.Retry();

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RedeliverAsync(long id, CancellationToken cancellationToken)
    {
        var message = await context.OutboxMessages
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Outbox-сообщение с Id '{id}' не найдено.");

        var brokerMessage = new BrokerMessage(message.EventId, message.Type, message.Content);

        await publisher.PublishAsync(brokerMessage, cancellationToken);
    }
}