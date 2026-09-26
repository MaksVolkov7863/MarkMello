using CommunityToolkit.Mvvm.Input;
using MarkMello.Application.Updates;
using MarkMello.Presentation.Services;

namespace MarkMello.Presentation.ViewModels;

public partial class ShellViewModel
{
    private readonly DeferredUpdateCheck? _deferredUpdateCheck;
    private bool _manualUpdateCheckStarted;

    public bool HasAvailableUpdate => _availableUpdatePackage is not null;

    public string UpdateActionHint => _localization["UpdateAction"];

    internal async Task CheckForUpdatesAfterStartupAsync(CancellationToken cancellationToken)
    {
        if (_deferredUpdateCheck is null)
        {
            return;
        }

        try
        {
            var result = await _deferredUpdateCheck.WaitAsync(cancellationToken).ConfigureAwait(true);
            // A manual check owns its result, including failures and downloaded packages.
            if (!_manualUpdateCheckStarted && !cancellationToken.IsCancellationRequested)
            {
                ApplyUpdateCheckResult(result);
                RefreshUpdateCommandStates();
            }
        }
        catch (OperationCanceledException)
        {
            // Window closure or application shutdown ends this observer quietly.
        }
    }

    [RelayCommand]
    private void OpenUpdateSettings()
    {
        MarkSecondaryFeaturesReady();
        IsFindBarOpen = false;
        ShellOverlay = ShellOverlayKind.AppSettings;
    }

    private void ApplyUpdateCheckResult(UpdateCheckResult result)
    {
        _availableUpdatePackage = null;
        switch (result)
        {
            case UpdateCheckResult.SourceNotConfigured:
                SetUpdateStatus(new UpdateStatusSnapshot.SourceNotConfiguredState());
                break;
            case UpdateCheckResult.UnsupportedPlatform unsupported:
                SetUpdateStatus(new UpdateStatusSnapshot.UnsupportedPlatformState(
                    unsupported.PlatformName, unsupported.ArchitectureName));
                break;
            case UpdateCheckResult.UpToDate current:
                SetUpdateStatus(new UpdateStatusSnapshot.UpToDateState(current.CurrentVersion, current.LatestVersion));
                break;
            case UpdateCheckResult.UpdateAvailable available:
                _availableUpdatePackage = available.Package;
                SetUpdateStatus(new UpdateStatusSnapshot.UpdateAvailableState(available.Package));
                break;
            case UpdateCheckResult.Failed failed:
                SetUpdateStatus(new UpdateStatusSnapshot.CheckFailedState(failed.Message));
                break;
        }
    }
}
