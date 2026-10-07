namespace DDD.Domain.Common;

public record PagedRequest(int PageNumber, int PageSize);
public record PagedResult<T>(IReadOnlyCollection<T> Items, int TotalCount);

public interface IRepository<TEntity, TKey> where TEntity : class, IEntity<TKey> where TKey : struct
{
    ValueTask<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken);
    Task<PagedResult<TEntity>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken);
    void Add(TEntity entity);
    void Remove(TEntity entity);
}