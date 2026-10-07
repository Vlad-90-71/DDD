namespace DDD.Domain.Common;

public interface IEntity { }

public interface IEntity<out TKey> where TKey : struct
{
    TKey Id { get; }
}
public abstract class Entity<TKey> : IEntity<TKey> where TKey : struct
{
    public TKey Id { get; protected set; } // или init
}