using System.Collections.Concurrent;

namespace Insurance.Api.Infrastructure;

/// <summary>
/// Thread-safe store for immutable entities. Entities are records that can't be mutated,
/// so returning stored instances directly is safe.
/// </summary>
public abstract class InMemoryRepository<T>
    where T : class
{
    private readonly ConcurrentDictionary<Guid, T> _items = new();
    private readonly Func<T, Guid> _getId;

    protected InMemoryRepository(Func<T, Guid> getId)
    {
        _getId = getId;
    }

    protected ICollection<T> Items => _items.Values;

    public T? FindById(Guid id) => _items.GetValueOrDefault(id);

    public void Save(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _items[_getId(entity)] = entity;
    }
}
