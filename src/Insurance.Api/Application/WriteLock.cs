namespace Insurance.Api.Application;

/// <summary>
/// Single process-wide lock for state-changing operations. Load, rule check and save must run inside one
/// <see cref="Execute{T}"/> call, otherwise two concurrent requests can both pass the same check
/// (e.g. two overlapping activations, or two decisions on one claim). Reads don't need it.
/// </summary>
public sealed class WriteLock
{
    private readonly Lock _lock = new();

    public T Execute<T>(Func<T> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        lock (_lock)
        {
            return operation();
        }
    }
}
