using MarkMello.Application.Updates;

namespace MarkMello.Presentation.Tests;

public sealed partial class ShellViewModelTests
{
    [Fact]
    public async Task DelayedDiscoveryShowsNotificationWithoutOpeningSettings()
    {
        var harness = CreateHarness(updateDelay: TimeSpan.Zero);
        harness.UpdateService.NextCheckResult = new UpdateCheckResult.UpdateAvailable(CreateUpdatePackage());

        await harness.ViewModel.CheckForUpdatesAfterStartupAsync(CancellationToken.None);

        Assert.True(harness.ViewModel.HasAvailableUpdate);
        Assert.True(harness.ViewModel.CanDownloadAvailableUpdate);
        Assert.False(harness.ViewModel.HasOpenOverlay);
        Assert.False(harness.ViewModel.IsCheckingForUpdates);
        Assert.Equal("Update", harness.ViewModel.UpdateActionHint);
        harness.ViewModel.OpenUpdateSettingsCommand.Execute(null);
        Assert.True(harness.ViewModel.IsAppSettingsOpen);
    }

    [Fact]
    public async Task DelayedDiscoveryKeepsNotificationHiddenWhenCurrentOrFailed()
    {
        foreach (var result in new UpdateCheckResult[]
        {
            new UpdateCheckResult.UpToDate("1.2.3", "1.2.3", DateTimeOffset.UtcNow, "https://github.com"),
            new UpdateCheckResult.Failed("Offline"),
            new UpdateCheckResult.SourceNotConfigured("Unavailable"),
            new UpdateCheckResult.UnsupportedPlatform("Linux", "arm64")
        })
        {
            var harness = CreateHarness(updateDelay: TimeSpan.Zero);
            harness.UpdateService.NextCheckResult = result;

            await harness.ViewModel.CheckForUpdatesAfterStartupAsync(CancellationToken.None);

            Assert.False(harness.ViewModel.HasAvailableUpdate);
            Assert.False(harness.ViewModel.HasOpenOverlay);
        }
    }

    [Fact]
    public async Task DelayedDiscoveryDoesNotOverwriteAManuallyDownloadedUpdate()
    {
        var harness = CreateHarness(updateDelay: TimeSpan.Zero);
        var package = CreateUpdatePackage();
        harness.UpdateService.NextCheckResult = new UpdateCheckResult.UpdateAvailable(package);
        harness.UpdateService.NextDownloadResult = new UpdateDownloadResult.Success(package, "update.exe");
        await harness.ViewModel.CheckForUpdatesCommand.ExecuteAsync(null);
        await harness.ViewModel.DownloadUpdateCommand.ExecuteAsync(null);
        harness.UpdateService.NextCheckResult = new UpdateCheckResult.Failed("Offline");

        await harness.ViewModel.CheckForUpdatesAfterStartupAsync(CancellationToken.None);

        Assert.True(harness.ViewModel.HasAvailableUpdate);
        Assert.True(harness.ViewModel.CanOpenDownloadedUpdate);
        Assert.Equal("update.exe", harness.ViewModel.DownloadedUpdatePath);
        Assert.Equal("Update ready", harness.ViewModel.UpdateStatusTitle);
    }

    [Fact]
    public async Task UpdateNotificationCanOpenSettingsWhileEditingWithoutChangingTheDocument()
    {
        var harness = CreateHarness();
        await harness.ViewModel.CreateNewDocumentCommand.ExecuteAsync(null);
        harness.ViewModel.EditorSession!.SourceText = "Unsaved content";

        harness.ViewModel.OpenUpdateSettingsCommand.Execute(null);

        Assert.True(harness.ViewModel.IsEditMode);
        Assert.True(harness.ViewModel.IsAppSettingsOpen);
        Assert.True(harness.ViewModel.IsAppOverlayOpen);
        Assert.True(harness.ViewModel.IsDirty);
        Assert.Equal("Unsaved content", harness.ViewModel.EditorSession.SourceText);
    }

    [Fact]
    public async Task CheckForUpdatesCommandWhenUpdateAvailableShowsDownloadAction()
    {
        var harness = CreateHarness();
        var package = CreateUpdatePackage();
        harness.UpdateService.NextCheckResult = new UpdateCheckResult.UpdateAvailable(package);

        await harness.ViewModel.CheckForUpdatesCommand.ExecuteAsync(null);

        Assert.Equal("Update 1.2.3 available", harness.ViewModel.UpdateStatusTitle);
        Assert.Contains(package.AssetName, harness.ViewModel.UpdateStatusMessage, StringComparison.Ordinal);
        Assert.True(harness.ViewModel.CanDownloadAvailableUpdate);
        Assert.False(harness.ViewModel.CanOpenDownloadedUpdate);
        Assert.Equal("Available", harness.ViewModel.UpdateStateBadge);
    }

    [Fact]
    public async Task DownloadUpdateCommandWhenSuccessfulShowsNativeAction()
    {
        var harness = CreateHarness();
        var package = CreateUpdatePackage();
        var downloadedPath = Path.Combine(Path.GetTempPath(), "MarkMello.Tests", package.AssetName);
        harness.UpdateService.NextCheckResult = new UpdateCheckResult.UpdateAvailable(package);
        harness.UpdateService.NextDownloadResult = new UpdateDownloadResult.Success(package, downloadedPath);

        await harness.ViewModel.CheckForUpdatesCommand.ExecuteAsync(null);
        await harness.ViewModel.DownloadUpdateCommand.ExecuteAsync(null);

        Assert.Equal("Update ready", harness.ViewModel.UpdateStatusTitle);
        Assert.Contains(package.AssetName, harness.ViewModel.UpdateStatusMessage, StringComparison.Ordinal);
        Assert.False(harness.ViewModel.CanDownloadAvailableUpdate);
        Assert.True(harness.ViewModel.CanOpenDownloadedUpdate);
        Assert.Equal("Launch installer", harness.ViewModel.DownloadedUpdateActionLabel);
        Assert.Equal(downloadedPath, harness.ViewModel.DownloadedUpdatePath);
        Assert.Equal("Ready", harness.ViewModel.UpdateStateBadge);
    }

    [Fact]
    public async Task OpenDownloadedUpdateCommandWhenSuccessfulUpdatesStatus()
    {
        var harness = CreateHarness();
        var package = CreateUpdatePackage();
        var downloadedPath = Path.Combine(Path.GetTempPath(), "MarkMello.Tests", package.AssetName);
        harness.UpdateService.NextCheckResult = new UpdateCheckResult.UpdateAvailable(package);
        harness.UpdateService.NextDownloadResult = new UpdateDownloadResult.Success(package, downloadedPath);
        harness.UpdateService.NextPrepareResult =
            new UpdatePrepareResult.Success("Installer launched. Follow the native upgrade flow.");

        await harness.ViewModel.CheckForUpdatesCommand.ExecuteAsync(null);
        await harness.ViewModel.DownloadUpdateCommand.ExecuteAsync(null);
        await harness.ViewModel.OpenDownloadedUpdateCommand.ExecuteAsync(null);

        Assert.Equal("Native update flow started", harness.ViewModel.UpdateStatusTitle);
        Assert.Equal(
            "Installer launched. Follow the native upgrade flow.",
            harness.ViewModel.UpdateStatusMessage);
    }

}
