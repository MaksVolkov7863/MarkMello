using MarkMello.Presentation.Editing;

namespace MarkMello.Presentation.Tests;

public sealed class MarkdownSourceBlockLocatorTests
{
    [Fact]
    public void TryExtractLinesSingleLineDocumentExtractsCorrectly()
    {
        var source = "Single line of text";
        var success = MarkdownSourceBlockLocator.TryExtractLines(source, 0, 0, out var blockText, out var charStart, out var charEnd);

        Assert.True(success);
        Assert.Equal("Single line of text", blockText);
        Assert.Equal(0, charStart);
        Assert.Equal(source.Length, charEnd);
    }

    [Fact]
    public void TryExtractLinesMultilineLfDocumentExtractsMiddleBlock()
    {
        var source = "Line 0\nLine 1\nLine 2\nLine 3";
        var success = MarkdownSourceBlockLocator.TryExtractLines(source, 1, 2, out var blockText, out var charStart, out var charEnd);

        Assert.True(success);
        Assert.Equal("Line 1\nLine 2", blockText);
        Assert.Equal(7, charStart);
        Assert.Equal(20, charEnd);
    }

    [Fact]
    public void TryExtractLinesMultilineCrlfDocumentExtractsMiddleBlock()
    {
        var source = "Line 0\r\nLine 1\r\nLine 2\r\nLine 3";
        var success = MarkdownSourceBlockLocator.TryExtractLines(source, 1, 2, out var blockText, out var charStart, out var charEnd);

        Assert.True(success);
        Assert.Equal("Line 1\r\nLine 2", blockText);
        Assert.Equal(8, charStart);
        Assert.Equal(22, charEnd);
    }

    [Fact]
    public void ReplaceRangeReplacesTextPreservingLineEndings()
    {
        var source = "# Header\r\n\r\nOriginal paragraph.\r\n\r\nFooter.";
        var success = MarkdownSourceBlockLocator.TryExtractLines(source, 2, 2, out _, out var charStart, out var charEnd);

        Assert.True(success);
        var result = MarkdownSourceBlockLocator.ReplaceRange(source, charStart, charEnd, "Updated paragraph line 1.\nLine 2.");

        Assert.Equal("# Header\r\n\r\nUpdated paragraph line 1.\r\nLine 2.\r\n\r\nFooter.", result);
    }

    [Fact]
    public void ReplaceRangeWorksForSingleLineNoBreaks()
    {
        var source = "Hello world";
        var result = MarkdownSourceBlockLocator.ReplaceRange(source, 6, 11, "MarkMello");

        Assert.Equal("Hello MarkMello", result);
    }
}
