using Microsoft.EntityFrameworkCore;
using DDD.Domain.Common;

namespace DDD.Infrastructure.Repositories;

public class Repository<TEntity, TKey>(DbContext context) : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : struct
{
    private readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();

    public ValueTask<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken) =>
        _dbSet.FindAsync([id], cancellationToken);

    public async Task<PagedResult<TEntity>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var totalCount = await _dbSet.CountAsync(cancellationToken);

        var items = await _dbSet
            .AsNoTracking()
            .OrderBy(e => e.Id) // Порядок обязателен для корректной пагинации!
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<TEntity>(items, totalCount);
    }
    public void Add(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _dbSet.Add(entity);
    }

    public void Remove(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _dbSet.Remove(entity);
    }
}