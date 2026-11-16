using Insurance.Api.Application;
using Insurance.Api.Tests.TestSupport;

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
        var overlapDetected = 0;

        await Concurrently.RunAsync(16, _ => writeLock.Execute(() =>
        {
            if (Interlocked.Increment(ref inside) > 1)
            {
                Interlocked.Exchange(ref overlapDetected, 1);
            }

            Thread.SpinWait(10_000);
            return Interlocked.Decrement(ref inside);
        }));

        Assert.Equal(0, overlapDetected);
    }
}
