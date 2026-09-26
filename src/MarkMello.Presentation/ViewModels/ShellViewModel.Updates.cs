using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarkMello.Application.Abstractions;
using MarkMello.Application.Updates;

namespace MarkMello.Presentation.ViewModels;

public partial class ShellViewModel
{
    private readonly IUpdateService _updateService;
    private AppUpdatePackage? _availableUpdatePackage;

    [ObservableProperty]
    private bool _isCheckingForUpdates;

    [ObservableProperty]
    private bool _isDownloadingUpdate;

    [ObservableProperty]
    private string _updateStatusTitle = string.Empty;

    [ObservableProperty]
    private string _updateStatusMessage = string.Empty;

    [ObservableProperty]
    private string? _downloadedUpdatePath;

    public bool CanCheckForUpdates => !IsCheckingForUpdates && !IsDownloadingUpdate;

    public bool CanDownloadAvailableUpdate
        => _availableUpdatePackage is not null
           && string.IsNullOrWhiteSpace(DownloadedUpdatePath)
           && !IsCheckingForUpdates
           && !IsDownloadingUpdate;

    public bool CanOpenDownloadedUpdate
        => _availableUpdatePackage is not null
           && !string.IsNullOrWhiteSpace(DownloadedUpdatePath)
           && !IsCheckingForUpdates
           && !IsDownloadingUpdate;

    public string CheckForUpdatesLabel => IsCheckingForUpdates ? _localization["UpdateChecking"] : _localization["UpdateCheckNow"];

    public string DownloadUpdateLabel => IsDownloadingUpdate ? _localization["UpdateDownloading"] : _localization["UpdateDownload"];

    public string DownloadedUpdateActionLabel
        => _availableUpdatePackage?.InstallAction switch
        {
            AppUpdateInstallAction.LaunchInstaller => _localization["UpdateLaunchInstaller"],
            AppUpdateInstallAction.OpenDiskImage => _localization["UpdateOpenDmg"],
            AppUpdateInstallAction.RevealFile => _localization["UpdateRevealAppImage"],
            _ => _localization["UpdateOpenDownloaded"]
        };

    public string UpdateStateBadge
        => IsCheckingForUpdates
            ? _localization["UpdateBadgeChecking"]
            : IsDownloadingUpdate
                ? _localization["UpdateBadgeDownloading"]
                : CanOpenDownloadedUpdate
                    ? _localization["UpdateBadgeReady"]
                    : CanDownloadAvailableUpdate
                        ? _localization["UpdateBadgeAvailable"]
                        : _localization["UpdateBadgeManual"];

    [RelayCommand(CanExecute = nameof(CanCheckForUpdates))]
    private async Task CheckForUpdatesAsync()
    {
        _manualUpdateCheckStarted = true;
        IsCheckingForUpdates = true;
        IsDownloadingUpdate = false;
        _availableUpdatePackage = null;
        DownloadedUpdatePath = null;
        SetUpdateStatus(new UpdateStatusSnapshot.CheckingState());
        UpdateCommandStates();

        try
        {
            var result = await _updateService.CheckForUpdatesAsync().ConfigureAwait(true);
            ApplyUpdateCheckResult(result);
        }
        finally
        {
            IsCheckingForUpdates = false;
            UpdateCommandStates();
        }
    }

    [RelayCommand(CanExecute = nameof(CanDownloadAvailableUpdate))]
    private async Task DownloadUpdateAsync()
    {
        if (_availableUpdatePackage is null)
        {
            return;
        }

        IsDownloadingUpdate = true;
        SetUpdateStatus(new UpdateStatusSnapshot.DownloadingState(_availableUpdatePackage));
        UpdateCommandStates();

        try
        {
            var result = await _updateService
                .DownloadUpdateAsync(_availableUpdatePackage)
                .ConfigureAwait(true);

            switch (result)
            {
                case UpdateDownloadResult.Success success:
                    _availableUpdatePackage = success.Package;
                    DownloadedUpdatePath = success.DownloadedFilePath;
                    SetUpdateStatus(new UpdateStatusSnapshot.DownloadReadyState(success.Package, success.DownloadedFilePath));
                    break;

                case UpdateDownloadResult.Failed failed:
                    DownloadedUpdatePath = null;
                    SetUpdateStatus(new UpdateStatusSnapshot.DownloadFailedState(failed.Message));
                    break;
            }
        }
        finally
        {
            IsDownloadingUpdate = false;
            UpdateCommandStates();
        }
    }

    [RelayCommand(CanExecute = nameof(CanOpenDownloadedUpdate))]
    private async Task OpenDownloadedUpdateAsync()
    {
        if (_availableUpdatePackage is null || string.IsNullOrWhiteSpace(DownloadedUpdatePath))
        {
            return;
        }

        var result = await _updateService
            .PrepareDownloadedUpdateAsync(_availableUpdatePackage, DownloadedUpdatePath)
            .ConfigureAwait(true);

        switch (result)
        {
            case UpdatePrepareResult.Success:
                SetUpdateStatus(new UpdateStatusSnapshot.NativeFlowStartedState(_availableUpdatePackage));
                break;

            case UpdatePrepareResult.Failed failed:
                SetUpdateStatus(new UpdateStatusSnapshot.OpenDownloadedFailedState(failed.Message));
                break;
        }

        UpdateCommandStates();
    }

    private void RefreshUpdateCommandStates()
    {
        CheckForUpdatesCommand.NotifyCanExecuteChanged();
        DownloadUpdateCommand.NotifyCanExecuteChanged();
        OpenDownloadedUpdateCommand.NotifyCanExecuteChanged();

        OnPropertyChanged(nameof(CanCheckForUpdates));
        OnPropertyChanged(nameof(CanDownloadAvailableUpdate));
        OnPropertyChanged(nameof(CanOpenDownloadedUpdate));
        OnPropertyChanged(nameof(CheckForUpdatesLabel));
        OnPropertyChanged(nameof(DownloadUpdateLabel));
        OnPropertyChanged(nameof(DownloadedUpdateActionLabel));
        OnPropertyChanged(nameof(UpdateStateBadge));
        OnPropertyChanged(nameof(HasAvailableUpdate));
    }
}