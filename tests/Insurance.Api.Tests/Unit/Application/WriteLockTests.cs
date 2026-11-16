using Insurance.Api.Application;

namespace Insurance.Api.Tests.Unit.Application;

public class WriteLockTests
{
    [Fact]
    public void Execute_WhenOperationGiven_ThenReturnsItsResult()
    {
        var writeLock = new WriteLock();

        Assert.Equal(42, writeLock.Execute(() => 42));
    }

    [Fact]
    public void Execute_WhenOperationNull_ThenThrows()
    {
        var writeLock = new WriteLock();

        Assert.Throws<ArgumentNullException>(() => writeLock.Execute<int>(null!));
    }

    [Fact]
    public async Task Execute_WhenCalledConcurrently_ThenOperationsDoNotInterleave()
    {
        var writeLock = new WriteLock();
        var inside = 0;
        var maxInside = 0;

        await Task.WhenAll(Enumerable.Range(0, 50).Select(_ => Task.Run(() => writeLock.Execute(() =>
        {
            var current = Interlocked.Increment(ref inside);
            maxInside = Math.Max(maxInside, current);
            Thread.SpinWait(10_000);
            Interlocked.Decrement(ref inside);
            return current;
        }))));

        Assert.Equal(1, maxInside);
    }
}
