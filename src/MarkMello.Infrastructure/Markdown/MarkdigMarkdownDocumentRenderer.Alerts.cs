using Markdig.Extensions.Alerts;
using Markdig.Syntax;
using MarkMello.Domain;

namespace MarkMello.Infrastructure.Markdown;

public sealed partial class MarkdigMarkdownDocumentRenderer
{
    private static MarkdownQuoteBlock ConvertQuote(QuoteBlock quote, string source)
    {
        var blocks = ConvertBlocks(quote, source);
        if (quote is not AlertBlock alert)
        {
            return new MarkdownQuoteBlock(blocks);
        }

        var kind = alert.Kind.ToString().ToUpperInvariant() switch
        {
            "NOTE" => MarkdownAlertKind.Note,
            "TIP" => MarkdownAlertKind.Tip,
            "IMPORTANT" => MarkdownAlertKind.Important,
            "WARNING" => MarkdownAlertKind.Warning,
            "CAUTION" => MarkdownAlertKind.Caution,
            _ => (MarkdownAlertKind?)null,
        };

        // Markdig consumes the marker line. Keep a selectable title in the same
        // block tree as the body so search, copying and edit scroll sync keep working.
        var title = kind?.ToString() ?? $"[!{alert.Kind}]";
        if (blocks.Count > 0 && blocks[0] is MarkdownParagraphBlock first)
        {
            if (first.Inlines.Count == 0)
            {
                blocks.RemoveAt(0);
            }
            else if (first.SourceSpan is { } span && span.StartLine == alert.Line)
            {
                blocks[0] = first with
                {
                    SourceSpan = new MarkdownSourceSpan(Math.Min(span.StartLine + 1, span.EndLine), span.EndLine),
                };
            }
        }

        blocks.Insert(0, new MarkdownParagraphBlock([new MarkdownTextInline(title)])
        {
            SourceSpan = new MarkdownSourceSpan(alert.Line),
        });
        return new MarkdownQuoteBlock(blocks) { AlertKind = kind };
    }
}
