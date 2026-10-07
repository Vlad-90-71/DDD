using DDD.Eventing.Contracts;

namespace DDD.Infrastructure;

public sealed class FailingUnitOfWork(IUnitOfWork inner) : IUnitOfWork
{
    private bool _failed;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (!_failed)
        {
            _failed = true;

            throw new InvalidOperationException(
                "TEST: намеренная ошибка SaveChangesAsync.");
        }

        return await inner.SaveChangesAsync(cancellationToken);
    }
}