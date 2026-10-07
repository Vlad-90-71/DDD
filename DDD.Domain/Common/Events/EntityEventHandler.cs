namespace DDD.Domain.Common.Events;

public abstract class EntityEventHandler<TEntity, TKey, TEvent>(
    IRepository<TEntity, TKey> repo,
    Func<TEvent, (TKey Id, Action<TEntity> Action)> mapper)
    : IDomainEventHandler<TEvent>
    where TEntity : class, IEntity<TKey>, IEntityEvent
    where TKey : struct
    where TEvent : IDomainEvent
{
    public async Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken)
    {
        var (id, action) = mapper(domainEvent);

        var entity = await repo.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Сущность с Id '{id}' не найдена.");

        action(entity);
    }
}