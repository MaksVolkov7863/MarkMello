using Avalonia.Controls.ApplicationLifetimes;
using MarkMello.Domain;
using MarkMello.Presentation.Services;

namespace MarkMello.Presentation.Views;

public partial class MainWindow
{
    private int _appliedInterfaceFontSize = ReadingPreferences.DefaultInterfaceFontSize;

    private void ApplyInterfaceFontSize()
    {
        var fontSize = _viewModel.ReadingPreferences.InterfaceFontSize;
        if (_appliedInterfaceFontSize == fontSize || global::Avalonia.Application.Current is not { } app)
        {
            return;
        }

        _appliedInterfaceFontSize = fontSize;
        InterfaceTypography.Apply(app.Resources, fontSize);
        if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Application resources are shared; keep other open settings panels in sync.
            foreach (var window in desktop.Windows.OfType<MainWindow>())
            {
                if (window._viewModel is { } shell && shell.ReadingPreferences.InterfaceFontSize != fontSize)
                {
                    shell.ReadingPreferences = shell.ReadingPreferences with { InterfaceFontSize = fontSize };
                }
            }
        }
    }
}
