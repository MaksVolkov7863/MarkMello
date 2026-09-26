using Avalonia;
using Avalonia.Media;
using MarkMello.Presentation.Controls;

namespace MarkMello.Presentation.Tests;

[Collection(AvaloniaHeadlessTestGroup.Name)]
public sealed class OverflowFadeTextBlockTests(AvaloniaHeadlessFixture fixture)
{
    [Fact]
    public Task FadeTracksTextOverflowAndViewportChanges()
    {
        return fixture.RunAsync(() =>
        {
            var title = new OverflowFadeTextBlock
            {
                Text = "implementation-plan-folders-tabs.md",
                FontSize = 12,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.None
            };

            Arrange(title, 80);
            Assert.NotNull(title.OpacityMask);

            Arrange(title, 500);
            Assert.Null(title.OpacityMask);

            Arrange(title, 80);
            Assert.NotNull(title.OpacityMask);

            title.Text = "a.md";
            Arrange(title, 80);
            Assert.Null(title.OpacityMask);
            return Task.CompletedTask;
        });
    }

    private static void Arrange(OverflowFadeTextBlock title, double width)
    {
        title.Measure(new Size(width, 34));
        title.Arrange(new Rect(0, 0, width, 34));
    }
}
