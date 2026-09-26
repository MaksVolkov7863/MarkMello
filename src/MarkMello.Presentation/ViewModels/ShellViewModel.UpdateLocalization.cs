using MarkMello.Application.Updates;

namespace MarkMello.Presentation.ViewModels;

public partial class ShellViewModel
{
    private UpdateStatusSnapshot _updateStatus = UpdateStatusSnapshot.Default;

    private void SetUpdateStatus(UpdateStatusSnapshot status)
    {
        _updateStatus = status;
        RefreshUpdateStatusTexts();
    }

    private void RefreshUpdateStatusTexts()
    {
        UpdateStatusTitle = _updateStatus switch
        {
            UpdateStatusSnapshot.DefaultState => _localization["UpdateDefaultTitle"],
            UpdateStatusSnapshot.CheckingState => _localization["UpdateCheckingTitle"],
            UpdateStatusSnapshot.SourceNotConfiguredState => _localization["UpdateUnavailableTitle"],
            UpdateStatusSnapshot.UnsupportedPlatformState => _localization["UpdateUnsupportedPlatformTitle"],
            UpdateStatusSnapshot.UpToDateState => _localization["UpdateUpToDateTitle"],
            UpdateStatusSnapshot.UpdateAvailableState available => _localization.Format("UpdateAvailableTitle", available.Package.ReleaseVersion),
            UpdateStatusSnapshot.CheckFailedState => _localization["UpdateCheckFailedTitle"],
            UpdateStatusSnapshot.DownloadingState downloading => _localization.Format("UpdateDownloadTitle", downloading.Package.ReleaseVersion),
            UpdateStatusSnapshot.DownloadReadyState => _localization["UpdateReadyTitle"],
            UpdateStatusSnapshot.DownloadFailedState => _localization["UpdateDownloadFailedTitle"],
            UpdateStatusSnapshot.NativeFlowStartedState => _localization["UpdateNativeFlowStartedTitle"],
            UpdateStatusSnapshot.OpenDownloadedFailedState => _localization["UpdateOpenDownloadedFailedTitle"],
            _ => _localization["UpdateDefaultTitle"]
        };

        UpdateStatusMessage = _updateStatus switch
        {
            UpdateStatusSnapshot.DefaultState => _localization["UpdateDefaultMessage"],
            UpdateStatusSnapshot.CheckingState => _localization["UpdateCheckingMessage"],
            UpdateStatusSnapshot.SourceNotConfiguredState => _localization["UpdateUnavailableMessage"],
            UpdateStatusSnapshot.UnsupportedPlatformState unsupported => _localization.Format(
                "UpdateUnsupportedPlatformMessage",
                unsupported.PlatformName,
                unsupported.ArchitectureName),
            UpdateStatusSnapshot.UpToDateState upToDate => _localization.Format(
                "UpdateUpToDateMessage",
                upToDate.CurrentVersion,
                upToDate.LatestVersion),
            UpdateStatusSnapshot.UpdateAvailableState available => _localization.Format(
                "UpdateAvailableMessage",
                available.Package.AssetName,
                available.Package.PlatformName,
                available.Package.ArchitectureName),
            UpdateStatusSnapshot.CheckFailedState failed => failed.Details,
            UpdateStatusSnapshot.DownloadingState downloading => _localization.Format(
                "UpdateDownloadMessage",
                downloading.Package.AssetName),
            UpdateStatusSnapshot.DownloadReadyState ready => GetUpdateReadyMessage(ready.Package, ready.DownloadedFilePath),
            UpdateStatusSnapshot.DownloadFailedState failed => failed.Details,
            UpdateStatusSnapshot.NativeFlowStartedState started => GetNativeFlowStartedMessage(started.Package),
            UpdateStatusSnapshot.OpenDownloadedFailedState failed => failed.Details,
            _ => _localization["UpdateDefaultMessage"]
        };
    }


    private string GetUpdateReadyMessage(AppUpdatePackage package, string downloadedFilePath)
    {
        var downloadedFileName = Path.GetFileName(downloadedFilePath);

        return package.InstallAction switch
        {
            AppUpdateInstallAction.LaunchInstaller => _localization.Format("UpdateReadyLaunchInstaller", downloadedFileName),
            AppUpdateInstallAction.OpenDiskImage => _localization.Format("UpdateReadyOpenDmg", downloadedFileName),
            AppUpdateInstallAction.RevealFile => _localization.Format("UpdateReadyRevealAppImage", downloadedFileName),
            _ => _localization.Format("UpdateReadyGeneric", downloadedFileName)
        };
    }

    private string GetNativeFlowStartedMessage(AppUpdatePackage package)
        => package.InstallAction switch
        {
            AppUpdateInstallAction.LaunchInstaller => _localization["UpdateNativeFlowStartedLaunchInstaller"],
            AppUpdateInstallAction.OpenDiskImage => _localization["UpdateNativeFlowStartedOpenDmg"],
            AppUpdateInstallAction.RevealFile => _localization["UpdateNativeFlowStartedRevealAppImage"],
            _ => _localization["UpdateOpenDownloaded"]
        };


    private abstract record UpdateStatusSnapshot
    {
        public static readonly UpdateStatusSnapshot Default = new DefaultState();

        public sealed record DefaultState : UpdateStatusSnapshot;

        public sealed record CheckingState : UpdateStatusSnapshot;

        public sealed record SourceNotConfiguredState : UpdateStatusSnapshot;

        public sealed record UnsupportedPlatformState(string PlatformName, string ArchitectureName) : UpdateStatusSnapshot;

        public sealed record UpToDateState(string CurrentVersion, string LatestVersion) : UpdateStatusSnapshot;

        public sealed record UpdateAvailableState(AppUpdatePackage Package) : UpdateStatusSnapshot;

        public sealed record CheckFailedState(string Details) : UpdateStatusSnapshot;

        public sealed record DownloadingState(AppUpdatePackage Package) : UpdateStatusSnapshot;

        public sealed record DownloadReadyState(AppUpdatePackage Package, string DownloadedFilePath) : UpdateStatusSnapshot;

        public sealed record DownloadFailedState(string Details) : UpdateStatusSnapshot;

        public sealed record NativeFlowStartedState(AppUpdatePackage Package) : UpdateStatusSnapshot;

        public sealed record OpenDownloadedFailedState(string Details) : UpdateStatusSnapshot;
    }
}
