using MarkMello.Application.Abstractions;
using MarkMello.Application.Updates;

namespace MarkMello.Presentation.Services;

/// <summary>One delayed discovery shared by all windows in this application run.</summary>
public sealed class DeferredUpdateCheck : IDisposable
{
    private readonly IUpdateService _updateService;
    private readonly TimeSpan _delay;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly object _gate = new();
    private Task<UpdateCheckResult>? _check;

    public DeferredUpdateCheck(IUpdateService updateService)
        : this(updateService, TimeSpan.FromSeconds(30))
    {
    }

    internal DeferredUpdateCheck(IUpdateService updateService, TimeSpan delay)
    {
        ArgumentNullException.ThrowIfNull(updateService);
        _updateService = updateService;
        _delay = delay;
    }

    public Task<UpdateCheckResult> WaitAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _check ??= CheckAsync(_shutdown.Token);
            return _check.WaitAsync(cancellationToken);
        }
    }

    private async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);
        try
        {
            return await _updateService.CheckForUpdatesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new UpdateCheckResult.Failed(exception.Message);
        }
    }

    public void Dispose()
    {
        _shutdown.Cancel();
        _shutdown.Dispose();
    }
}
