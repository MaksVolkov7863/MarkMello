using CommunityToolkit.Mvvm.Input;
using MarkMello.Domain;

namespace MarkMello.Presentation.ViewModels;

public partial class ShellViewModel
{
    public string InterfaceFontSizeLabel => _localization["InterfaceFontSizeLabel"];
    public string InterfaceFontSizeHint => _localization["InterfaceFontSizeHint"];
    public string ResetReadingSettingsLabel => _localization["ResetReadingSettingsLabel"];
    public string InterfaceFontSizeValueLabel => $"{ReadingPreferences.InterfaceFontSize}px";
    public static double InterfaceFontSizeMinimum => ReadingPreferences.MinInterfaceFontSize;
    public static double InterfaceFontSizeMaximum => ReadingPreferences.MaxInterfaceFontSize;

    public double InterfaceFontSizeSetting
    {
        get => ReadingPreferences.InterfaceFontSize;
        set => ApplyReadingPreferences(ReadingPreferences with
        {
            InterfaceFontSize = (int)Math.Round(value, MidpointRounding.AwayFromZero)
        });
    }

    [RelayCommand]
    private void ResetReadingSettings() => ApplyReadingPreferences(ReadingPreferences.Default);

    private void RefreshInterfaceTypographyProperties()
    {
        OnPropertyChanged(nameof(InterfaceFontSizeLabel));
        OnPropertyChanged(nameof(InterfaceFontSizeHint));
        OnPropertyChanged(nameof(InterfaceFontSizeValueLabel));
        OnPropertyChanged(nameof(InterfaceFontSizeSetting));
        OnPropertyChanged(nameof(ResetReadingSettingsLabel));
    }
}
