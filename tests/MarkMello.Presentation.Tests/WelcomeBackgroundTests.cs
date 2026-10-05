using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using MarkMello.Presentation.Views;

namespace MarkMello.Presentation.Tests;

[Collection(AvaloniaHeadlessTestGroup.Name)]
public sealed class WelcomeBackgroundTests(AvaloniaHeadlessFixture fixture)
{
    [Fact]
    public Task BackgroundFollowsThemeChangesWithoutRecreatingTheWelcomeView()
    {
        return fixture.RunAsync(() =>
        {
            var view = new WelcomeView();
            var window = new Window
            {
                Content = view,
                RequestedThemeVariant = ThemeVariant.Light
            };
            window.Show();
            try
            {
                var surface = view.FindControl<Grid>("WelcomeBackgroundSurface")!;
                var light = Assert.IsType<ImageBrush>(surface.Background);
                Assert.IsType<Bitmap>(light.Source);
                Assert.Equal(Stretch.UniformToFill, light.Stretch);

                window.RequestedThemeVariant = ThemeVariant.Dark;
                var dark = Assert.IsType<ImageBrush>(surface.Background);
                Assert.IsType<Bitmap>(dark.Source);
                Assert.NotSame(light.Source, dark.Source);
                Assert.Equal(Stretch.UniformToFill, dark.Stretch);

                window.RequestedThemeVariant = ThemeVariant.Light;
                Assert.Same(light, surface.Background);
            }
            finally
            {
                window.Close();
            }

            return Task.CompletedTask;
        });
    }
}
