using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;
using MarkMello.Domain;
using MarkMello.Presentation.Views;
using MarkMello.Presentation.Views.Markdown;

namespace MarkMello.Presentation.Tests;

[Collection(AvaloniaHeadlessTestGroup.Name)]
public sealed class AdvancedReadingRenderingTests(AvaloniaHeadlessFixture fixture)
{
    [Fact]
    public Task LetterSpacingExpandsCyrillicTextAndKeepsSelectionCoordinatesValid()
    {
        return fixture.RunAsync(() =>
        {
            var text = "Кириллица и латиница: readable text";
            var document = new RenderedMarkdownDocument(
                [new MarkdownParagraphBlock([new MarkdownTextInline(text)])]);
            var first = CreateFragment(document, 0);
            var spaced = CreateFragment(document, 1);

            first.Measure(new Size(double.PositiveInfinity, 500));
            spaced.Measure(new Size(double.PositiveInfinity, 500));

            Assert.True(spaced.DesiredSize.Width > first.DesiredSize.Width);
            spaced.Arrange(new Rect(0, 0, 1000, 500));
            Assert.Equal(text.Length, spaced.GetDocumentOffset(new Point(999, 10)));
            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task InstalledFontIsAppliedToBodyHeadingsAndCodeWithoutSpacingCodeBlocks()
    {
        return fixture.RunAsync(() =>
        {
            var name = FontManager.Current.SystemFonts[0].Name;
            var view = new MarkdownDocumentView
            {
                ReadingPreferences = ReadingPreferences.Default with
                {
                    SerifFontFamily = name, SansFontFamily = name, MonoFontFamily = name,
                    LetterSpacing = 1.2
                },
                Document = new RenderedMarkdownDocument(
                [
                    new MarkdownHeadingBlock(2, [new MarkdownTextInline("Заголовок")]),
                    new MarkdownParagraphBlock([new MarkdownTextInline("Текст "), new MarkdownCodeInline("code")]),
                    new MarkdownCodeBlock(null, "code")
                ])
            };
            var fragments = view.GetLogicalDescendants().OfType<MarkdownSelectionTextFragment>().ToArray();

            Assert.Equal(3, fragments.Length);
            Assert.All(fragments, fragment => Assert.Equal(name, fragment.BaseFontFamily.Name));
            Assert.Equal(name, fragments[1].InlineCodeFontFamily?.Name);
            Assert.Equal(1.2, fragments[1].BaseLetterSpacing);
            Assert.Equal(0, fragments[2].BaseLetterSpacing);
            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task MissingCustomFontUsesTheSameFallbackAsTheBuiltInFamily()
    {
        return fixture.RunAsync(() =>
        {
            var document = new RenderedMarkdownDocument(
                [new MarkdownParagraphBlock([new MarkdownTextInline("Текст")])]);
            var view = new MarkdownDocumentView
            {
                ReadingPreferences = ReadingPreferences.Default with { SerifFontFamily = "MarkMello Missing Font" },
                Document = document
            };
            var fallback = GetFragment(view);

            Assert.Equal(CreateFragment(document, 0).BaseFontFamily, fallback.BaseFontFamily);
            return Task.CompletedTask;
        });
    }

    private static MarkdownSelectionTextFragment CreateFragment(RenderedMarkdownDocument document, double spacing)
    {
        var view = new MarkdownDocumentView
        {
            ReadingPreferences = ReadingPreferences.Default with { LetterSpacing = spacing },
            Document = document
        };
        return GetFragment(view);
    }

    private static MarkdownSelectionTextFragment GetFragment(MarkdownDocumentView view)
        => Assert.IsType<StackPanel>(Assert.IsType<Border>(view.Content).Child)
            .Children.OfType<MarkdownSelectionTextFragment>().Single();
}
