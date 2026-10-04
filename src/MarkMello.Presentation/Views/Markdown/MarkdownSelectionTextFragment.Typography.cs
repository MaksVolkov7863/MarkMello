using Avalonia.Controls;
using Avalonia.Media;
using MarkMello.Domain;

namespace MarkMello.Presentation.Views.Markdown;

internal sealed partial class MarkdownSelectionTextFragment
{
    public FontFamily? InlineCodeFontFamily { get; init; }

    private MarkdownFormattedTextLayout GetOrCreateTextLayout(double availableWidth)
    {
        var normalizedWidth = NormalizeLayoutWidth(availableWidth);
        if (_textLayout is not null && Math.Abs(_layoutWidth - normalizedWidth) < 0.5)
        {
            return _textLayout;
        }

        InvalidateTextLayout();
        _layoutWidth = normalizedWidth;
        _textLayout = new MarkdownFormattedTextLayout(
            StyledText,
            _inlineImages,
            BaseFontFamily,
            InlineCodeFontFamily ?? ResolveInlineCodeFontFamily(),
            BaseFontSize,
            BaseFontWeight,
            BaseFontStyle,
            double.IsNaN(BaseLineHeight) ? double.NaN : BaseLineHeight,
            _letterSpacing,
            LayoutTextWrapping,
            normalizedWidth,
            ResolveBaseTextBrush(),
            BuildLinkTextDecorations());

        return _textLayout;
    }

    private FontFamily ResolveInlineCodeFontFamily()
    {
        if (this.TryFindResource("MmDocumentMonoFontFamily", ActualThemeVariant, out var value)
            && value is FontFamily family)
        {
            return family;
        }

        return new FontFamily("JetBrains Mono, Cascadia Code, Consolas, Menlo, monospace");
    }

}
