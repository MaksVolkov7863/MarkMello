using Avalonia.Controls;
using Avalonia.Interactivity;
using MarkMello.Presentation.ViewModels;

namespace MarkMello.Presentation.Views;

public partial class ReadingSettingsPanelView : UserControl
{
    public ReadingSettingsPanelView()
    {
        InitializeComponent();
    }

    private async void OnAdvancedSettingsClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ShellViewModel shell || TopLevel.GetTopLevel(this) is not Window owner)
        {
            return;
        }

        shell.CloseSettingsCommand.Execute(null);
        var dialog = new AdvancedReadingSettingsWindow
        {
            DataContext = shell,
            Width = Math.Min(592, owner.ClientSize.Width - 32),
            MaxHeight = Math.Max(280, owner.ClientSize.Height - 32)
        };
        await dialog.ShowDialog(owner).ConfigureAwait(true);
    }
}
