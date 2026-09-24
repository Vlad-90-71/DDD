using DDD.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DDD.Application.Common;

public interface IOutboxService
{
    Task RetryAsync(long id, CancellationToken cancellationToken);
}
public class OutboxService(AppDbContext context) : IOutboxService
{
    public async Task RetryAsync(long id, CancellationToken cancellationToken)
    {
        var message = await context.OutboxMessages
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken) 
                ?? throw new KeyNotFoundException($"Outbox-сообщение с Id '{id}' не найдено.");

        message.Retry();

        await context.SaveChangesAsync(cancellationToken);
    }
}