using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using DDD.MessageBroker.Contracts;

namespace DDD.Infrastructure.Outbox;

public interface IOutboxService
{
    Task RetryAsync(long id, CancellationToken cancellationToken);
    Task RedeliverAsync(long id, CancellationToken cancellationToken);
}

public sealed class OutboxService(
    AppDbContext context,
    IMessagePublisher publisher,
    ILogger<OutboxService> logger) : IOutboxService
{
    public async Task RetryAsync(long id, CancellationToken cancellationToken)
    {
        var message = await context.OutboxMessages
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken) 
                ?? throw new KeyNotFoundException($"Outbox-сообщение с Id '{id}' не найдено.");
        
        message.Retry();

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Outbox message retry scheduled. OutboxMessageId={OutboxMessageId}, EventId={EventId}, Type={EventType}",
            message.Id, message.EventId, message.Type);
    }

    public async Task RedeliverAsync(long id, CancellationToken cancellationToken)
    {
        var message = await context.OutboxMessages
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken) 
                ?? throw new KeyNotFoundException($"Outbox-сообщение с Id '{id}' не найдено.");
        
        var brokerMessage = new BrokerMessage(message.EventId, message.Type, message.Content);

        await publisher.PublishAsync(brokerMessage, cancellationToken);

        logger.LogInformation(
            "Outbox message manually redelivered. OutboxMessageId={OutboxMessageId}, EventId={EventId}, Type={EventType}",
            message.Id, message.EventId, message.Type);
    }
}