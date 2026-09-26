using MarkMello.Application.Updates;
using MarkMello.Presentation.Services;

namespace MarkMello.Presentation.Tests;

public sealed class DeferredUpdateCheckTests
{
    [Fact]
    public async Task StartupDelayDoesNotCallTheServiceImmediately()
    {
        var service = new StubUpdateService();
        using var check = new DeferredUpdateCheck(service);
        using var observer = new CancellationTokenSource();

        var pending = check.WaitAsync(observer.Token);

        Assert.False(pending.IsCompleted);
        Assert.Equal(0, service.CheckCount);
        observer.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    }

    [Fact]
    public async Task MultipleWindowsShareOneCheckAndItsResult()
    {
        var service = new StubUpdateService();
        using var check = new DeferredUpdateCheck(service, TimeSpan.FromMilliseconds(20));

        var first = check.WaitAsync(CancellationToken.None);
        var second = check.WaitAsync(CancellationToken.None);
        var results = await Task.WhenAll(first, second);

        Assert.Same(results[0], results[1]);
        Assert.Same(results[0], await check.WaitAsync(CancellationToken.None));
        Assert.Equal(1, service.CheckCount);
    }

    [Fact]
    public async Task ClosingOneWindowDoesNotCancelAnotherObserver()
    {
        var service = new StubUpdateService();
        using var check = new DeferredUpdateCheck(service, TimeSpan.FromMilliseconds(20));
        using var observer = new CancellationTokenSource();
        var closed = check.WaitAsync(observer.Token);
        var remaining = check.WaitAsync(CancellationToken.None);

        observer.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => closed);
        Assert.IsType<UpdateCheckResult.SourceNotConfigured>(await remaining);
        Assert.Equal(1, service.CheckCount);
    }

    [Fact]
    public async Task ShutdownCancelsPendingDiscoveryBeforeTheNetworkRequest()
    {
        var service = new StubUpdateService();
        var check = new DeferredUpdateCheck(service);
        var pending = check.WaitAsync(CancellationToken.None);

        check.Dispose();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(0, service.CheckCount);
    }
}
