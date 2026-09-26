using MarkMello.Application.Abstractions;
using MarkMello.Application.Updates;

namespace MarkMello.Presentation.Tests;

internal sealed class StubUpdateService : IUpdateService
{
    public UpdateCheckResult NextCheckResult { get; set; }
        = new UpdateCheckResult.SourceNotConfigured("Update source is not configured.");

    public UpdateDownloadResult NextDownloadResult { get; set; }
        = new UpdateDownloadResult.Failed("No downloaded update configured for this test.");

    public UpdatePrepareResult NextPrepareResult { get; set; }
        = new UpdatePrepareResult.Failed("No native handoff configured for this test.");

    public int CheckCount { get; private set; }

    public Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        CheckCount++;
        return Task.FromResult(NextCheckResult);
    }

    public Task<UpdateDownloadResult> DownloadUpdateAsync(
        AppUpdatePackage package,
        CancellationToken cancellationToken = default)
        => Task.FromResult(NextDownloadResult);

    public Task<UpdatePrepareResult> PrepareDownloadedUpdateAsync(
        AppUpdatePackage package,
        string downloadedFilePath,
        CancellationToken cancellationToken = default)
        => Task.FromResult(NextPrepareResult);
}
