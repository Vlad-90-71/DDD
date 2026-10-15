namespace DDD.Domain.Common;

public interface IId { }

public interface IId<out TKey> : IId where TKey : struct
{
    TKey Id { get; }
}
public interface IEntity { }
public abstract class Entity : Entity<int> { }
public abstract class Entity<TKey> : IEntity, IId<TKey> where TKey : struct
{
    public TKey Id { get; protected set; } // или init
}
