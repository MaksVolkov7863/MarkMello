using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MarkMello.Presentation.Views;

public partial class WelcomeView : UserControl
{
    private static readonly Uri SupportUri = new("https://markmello.ru/donate");

    public WelcomeView()
    {
        InitializeComponent();
    }

    private async void OnSupportClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this)?.Launcher is { } launcher)
        {
            await launcher.LaunchUriAsync(SupportUri).ConfigureAwait(true);
        }
    }
}
