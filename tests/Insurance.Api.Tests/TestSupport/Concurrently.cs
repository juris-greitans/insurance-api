namespace Insurance.Api.Tests.TestSupport;

internal static class Concurrently
{
    /// <summary>
    /// Runs <paramref name="count"/> operations on dedicated threads, released together by a barrier,
    /// so they actually overlap instead of running one after another.
    /// </summary>
    public static async Task<T[]> RunAsync<T>(int count, Func<int, T> operation)
    {
        using var barrier = new Barrier(count);

        var tasks = Enumerable.Range(0, count)
            .Select(i => Task.Factory.StartNew(
                () =>
                {
                    barrier.SignalAndWait();
                    return operation(i);
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default))
            .ToArray();

        return await Task.WhenAll(tasks);
    }
}
