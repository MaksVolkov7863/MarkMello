using MarkMello.Domain;
using MarkMello.Infrastructure.Markdown;

namespace MarkMello.Presentation.Tests;

public sealed class MarkdownAlertParsingTests
{
    [Theory]
    [InlineData("NOTE", MarkdownAlertKind.Note)]
    [InlineData("TIP", MarkdownAlertKind.Tip)]
    [InlineData("IMPORTANT", MarkdownAlertKind.Important)]
    [InlineData("WARNING", MarkdownAlertKind.Warning)]
    [InlineData("CAUTION", MarkdownAlertKind.Caution)]
    public void AlertPreservesItsTitleBodyAndSourceLines(string marker, MarkdownAlertKind kind)
    {
        var document = new MarkdigMarkdownDocumentRenderer().Render($"> [!{marker}]\n> Body **bold** [link](https://example.com)");
        var alert = Assert.IsType<MarkdownQuoteBlock>(Assert.Single(document.Blocks));
        Assert.Equal(kind, alert.AlertKind);
        Assert.Equal(new MarkdownSourceSpan(0, 1), alert.SourceSpan);
        var title = Assert.IsType<MarkdownParagraphBlock>(alert.Blocks[0]);
        Assert.Equal(kind.ToString(), MarkdownDocumentTextMap.ExtractPlainText(title));
        Assert.Equal(new MarkdownSourceSpan(0), title.SourceSpan);
        var body = Assert.IsType<MarkdownParagraphBlock>(alert.Blocks[1]);
        Assert.Equal(new MarkdownSourceSpan(1), body.SourceSpan);
        Assert.Contains(body.Inlines, inline => inline is MarkdownStrongInline);
        Assert.Contains(body.Inlines, inline => inline is MarkdownLinkInline);
        var text = MarkdownDocumentTextMap.Create(document).Text;
        Assert.Contains(kind.ToString(), text, StringComparison.Ordinal);
        Assert.Contains("Body bold link", text, StringComparison.Ordinal);
        Assert.DoesNotContain($"[!{marker}]", text, StringComparison.Ordinal);
    }

    [Fact]
    public void AlertKeepsMultipleParagraphsListsAndFencedCode()
    {
        var document = new MarkdigMarkdownDocumentRenderer().Render("""
            > [!NOTE]
            > First paragraph.
            >
            > Second paragraph.
            >
            > - First item
            > - Second item
            >
            > ```text
            > code
            > ```
            """);
        var alert = Assert.IsType<MarkdownQuoteBlock>(Assert.Single(document.Blocks));
        Assert.Equal(5, alert.Blocks.Count);
        Assert.IsType<MarkdownListBlock>(alert.Blocks[3]);
        Assert.IsType<MarkdownCodeBlock>(alert.Blocks[4]);
        Assert.Contains("Second paragraph.", MarkdownDocumentTextMap.Create(document).Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("> ordinary quote")]
    [InlineData("> \\[!WARNING]\n> escaped marker")]
    [InlineData("> `[!WARNING]`\n> inline code")]
    [InlineData("> [!WARNING] on the same line")]
    public void OrdinaryQuotesAndLiteralMarkersRemainQuotes(string markdown)
    {
        var document = new MarkdigMarkdownDocumentRenderer().Render(markdown);
        var quote = Assert.IsType<MarkdownQuoteBlock>(Assert.Single(document.Blocks));
        Assert.Null(quote.AlertKind);
    }

    [Fact]
    public void UnknownAlertKindKeepsItsMarkerAsVisibleText()
    {
        var document = new MarkdigMarkdownDocumentRenderer().Render("> [!EXPERIMENT]\n> Unknown body");
        var quote = Assert.IsType<MarkdownQuoteBlock>(Assert.Single(document.Blocks));
        Assert.Null(quote.AlertKind);
        Assert.Contains("[!EXPERIMENT]", MarkdownDocumentTextMap.Create(document).Text, StringComparison.Ordinal);
        Assert.Contains("Unknown body", MarkdownDocumentTextMap.Create(document).Text, StringComparison.Ordinal);
    }
}
