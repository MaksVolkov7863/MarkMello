using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using MarkMello.Presentation.Controls;

namespace MarkMello.Presentation.Tests;

[Collection(AvaloniaHeadlessTestGroup.Name)]
public sealed class WelcomePanelTests(AvaloniaHeadlessFixture fixture)
{
    [Theory]
    [InlineData(640, 444)]
    [InlineData(1280, 804)]
    public Task ContentMovesOnlyWhenItWouldOverlapSupport(double width, double height)
    {
        return fixture.RunAsync(() =>
        {
            var content = new StackPanel
            {
                Width = 380,
                Height = 360,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var support = new Button
            {
                Width = 180,
                Height = 32,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 40)
            };
            var panel = new WelcomePanel { Children = { content, support } };
            var size = new Size(width, height);

            panel.Measure(size);
            panel.Arrange(new Rect(size));

            Assert.Equal(height - 40, support.Bounds.Bottom);
            Assert.True(content.Bounds.Bottom <= support.Bounds.Top);
            Assert.Equal(Math.Min((height - 360) / 2, support.Bounds.Top - 360), content.Bounds.Top);

            // Growing the same panel must restore the original centered position.
            size = new Size(width, 1000);
            panel.Measure(size);
            panel.Arrange(new Rect(size));

            Assert.Equal(320, content.Bounds.Top);
            Assert.Equal(960, support.Bounds.Bottom);
            return Task.CompletedTask;
        });
    }
}
