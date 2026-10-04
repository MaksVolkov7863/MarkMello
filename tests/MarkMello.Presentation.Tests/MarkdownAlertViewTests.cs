using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using MarkMello.Domain;
using MarkMello.Infrastructure.Markdown;
using MarkMello.Presentation.Views;
using MarkMello.Presentation.Views.Markdown;

namespace MarkMello.Presentation.Tests;

[Collection(AvaloniaHeadlessTestGroup.Name)]
public sealed class MarkdownAlertViewTests(AvaloniaHeadlessFixture fixture)
{
    [Theory]
    [InlineData("NOTE", "#536F87", "#92AEC5")]
    [InlineData("TIP", "#5D7457", "#A1B696")]
    [InlineData("IMPORTANT", "#796788", "#B9A7CA")]
    [InlineData("WARNING", "#956C23", "#D4B270")]
    [InlineData("CAUTION", "#BD592F", "#E38A67")]
    public Task AlertUsesTheApprovedGeometryAndUpdatesItsPaletteWithTheTheme(
        string marker, string lightColor, string darkColor)
        => fixture.RunAsync(() =>
        {
            var view = CreateView($"> [!{marker}]\n> Body");
            var resources = (ResourceDictionary)AvaloniaXamlLoader.Load(
                new Uri("avares://MarkMello.Presentation/Themes/Colors.axaml"));
            var window = new Window
            {
                Content = view, Resources = resources,
                RequestedThemeVariant = ThemeVariant.Light,
                Width = 420, Height = 240,
            };
            window.Show();
            try
            {
                var alert = view.GetVisualDescendants().OfType<MarkdownAlertBlockView>().Single();
                var bar = alert.Children.OfType<Border>().Single(border => border.Width == 4);
                Assert.Equal(new CornerRadius(2), bar.CornerRadius);
                Assert.Equal(Color.Parse(lightColor), Assert.IsAssignableFrom<ISolidColorBrush>(bar.Background).Color);
                var fragments = alert.GetVisualDescendants().OfType<MarkdownSelectionTextFragment>().ToArray();
                Assert.Equal(FontStyle.Normal, fragments[1].BaseFontStyle);
                Assert.Equal(ReadingPreferences.Default.FontSize, fragments[1].BaseFontSize);
                Assert.Equal(14, fragments[0].BaseFontSize);

                window.RequestedThemeVariant = ThemeVariant.Dark;
                Assert.Equal(Color.Parse(darkColor), Assert.IsAssignableFrom<ISolidColorBrush>(bar.Background).Color);
                Assert.Equal(Color.Parse(darkColor), Assert.IsAssignableFrom<ISolidColorBrush>(fragments[0].BaseForeground).Color);
            }
            finally
            {
                window.Close();
            }
            return Task.CompletedTask;
        });

    [Fact]
    public Task AlertParticipatesInSelectionSearchAndIncrementalPreview()
        => fixture.RunAsync(() =>
        {
            var view = CreateView("> [!NOTE]\n> Body [link](https://example.com)");
            view.SelectAll();
            Assert.Contains("Note", view.SelectedText, StringComparison.Ordinal);
            Assert.Contains("Body link", view.SelectedText, StringComparison.Ordinal);
            view.ApplySearchQuery("Body");
            Assert.Equal(1, view.MatchCount);
            var oldAlert = GetAlert(view);

            view.Document = new MarkdigMarkdownDocumentRenderer().Render("> [!NOTE]\n> Body [link](https://example.com)");
            Assert.Same(oldAlert, GetAlert(view));
            view.Document = new MarkdigMarkdownDocumentRenderer().Render("> [!WARNING]\n> Body [link](https://example.com)");
            Assert.NotSame(oldAlert, GetAlert(view));
            Assert.Equal(1, view.MatchCount);
            return Task.CompletedTask;
        });

    private static MarkdownDocumentView CreateView(string markdown)
        => new()
        {
            Document = new MarkdigMarkdownDocumentRenderer().Render(markdown),
            ReadingPreferences = ReadingPreferences.Default,
        };

    private static MarkdownAlertBlockView GetAlert(MarkdownDocumentView view)
        => Assert.IsType<MarkdownAlertBlockView>(Assert.Single(
            Assert.IsType<StackPanel>(Assert.IsType<Border>(view.Content).Child).Children));
}
