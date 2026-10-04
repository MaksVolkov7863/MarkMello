using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using MarkMello.Domain;
using MarkMello.Presentation.Views.Markdown;

namespace MarkMello.Presentation.Views;

public sealed partial class MarkdownDocumentView
{
    private Control BuildQuote(MarkdownQuoteBlock block, string path)
    {
        if (block.AlertKind is { } kind
            && block.Blocks.Count > 0
            && block.Blocks[0] is MarkdownParagraphBlock title)
        {
            return BuildAlert(block, title, kind, path);
        }

        var stack = new StackPanel { Orientation = Orientation.Vertical };
        for (var index = 0; index < block.Blocks.Count; index++)
        {
            stack.Children.Add(BuildBlock(block.Blocks[index], $"{path}.b{index}", nested: true, insideQuote: true));
        }

        return new Border { Classes = { "mm-md-quote" }, Child = stack };
    }

    private MarkdownAlertBlockView BuildAlert(
        MarkdownQuoteBlock block,
        MarkdownParagraphBlock title,
        MarkdownAlertKind kind,
        string path)
    {
        var titleControl = BuildSelectionFragment(
            $"{path}.b0", title.Inlines, default, 14, 21,
            FontWeight.Medium, FontStyle.Normal, "mm-md-alert-title",
            baseFontFamily: ResolveSansFontFamily());
        RegisterSourceLineAnchor(title, titleControl);

        var body = new StackPanel();
        for (var index = 1; index < block.Blocks.Count; index++)
        {
            var child = BuildBlock(block.Blocks[index], $"{path}.b{index}", nested: true);
            if (index == block.Blocks.Count - 1)
            {
                child.Margin = new Thickness(child.Margin.Left, child.Margin.Top, child.Margin.Right, 0);
            }
            body.Children.Add(child);
        }

        return new MarkdownAlertBlockView(kind, titleControl, body);
    }
}
