namespace DDD.Domain.Common.Events;

public abstract class EntityEventHandler<TEntity, TKey, TEvent>(
    IRepository<TEntity, TKey> repo,
    Func<TEvent, (TKey Id, Action<TEntity> Action)> mapper)
    : DomainEventHandler<TEvent>(async (domainEvent, cancellationToken) =>
    {
        // Переносим логику обработки прямо в лямбду для базового класса
        var (id, action) = mapper(domainEvent);

        var entity = await repo.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Сущность с Id '{id}' не найдена.");

        action(entity);
    })
    where TEntity : Entity<TKey>, IEntityEvent
    where TKey : struct
    where TEvent : DomainEvent { }
