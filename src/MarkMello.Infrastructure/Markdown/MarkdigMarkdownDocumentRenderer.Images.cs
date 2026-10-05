using System.Net;
using System.Globalization;
using System.Text.RegularExpressions;
using Markdig;
using MarkdigMarkdown = Markdig.Markdown;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MarkMello.Application.Abstractions;
using MarkMello.Application.Diagrams;
using MarkMello.Domain;
using CodeBlock = Markdig.Syntax.CodeBlock;

namespace MarkMello.Infrastructure.Markdown;


public sealed partial class MarkdigMarkdownDocumentRenderer
{
    private static void HandleInlineHtmlTag(string tag, List<MarkdownInline> target)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return;
        }

        if (LineBreakTagPattern.IsMatch(tag))
        {
            // <br>, <br/>, <br /> should preserve the author's intended line break.
            target.Add(new MarkdownLineBreakInline());
            return;
        }

        if (ImgTagPattern.IsMatch(tag))
        {
            target.Add(new MarkdownTextInline(FormatImagePlaceholder(tag)));
            return;
        }

        // Generic container/opener/closer tags (<b>, </b>, <span>, ...): drop them.
        // The surrounding literal inlines already carry the visible text, so
        // skipping the tag text is equivalent to "strip tags, keep content".
    }

    private static string FormatImagePlaceholder(string imgTag)
    {
        var altMatch = AltAttrPattern.Match(imgTag);
        if (altMatch.Success)
        {
            var alt = altMatch.Groups[1].Success
                ? altMatch.Groups[1].Value
                : altMatch.Groups[2].Value;
            if (!string.IsNullOrWhiteSpace(alt))
            {
                return $"[image: {alt}]";
            }
        }

        return "[image]";
    }

    /// <summary>
    /// Detects the "figure" pattern: a paragraph whose only visible content
    /// is a single markdown image (![alt](url) optionally preceded/followed
    /// by whitespace or a line break). Returns the extracted image block.
    /// </summary>
    private static bool TryExtractStandaloneImage(
        ContainerInline? container,
        out MarkdownImageBlock imageBlock)
    {
        imageBlock = null!;
        if (container is null)
        {
            return false;
        }

        LinkInline? onlyImage = null;
        foreach (var inline in container)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    // Allow whitespace-only literals around the image.
                    var text = literal.Content.ToString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return false;
                    }
                    break;

                case LineBreakInline:
                    // A trailing soft break doesn't disqualify the figure pattern.
                    break;

                case LinkInline link when link.IsImage:
                    if (onlyImage is not null)
                    {
                        // Two or more images in the same paragraph -- keep
                        // them inline so the user's layout intent is clear.
                        return false;
                    }
                    onlyImage = link;
                    break;

                default:
                    // Anything else (emphasis, other links, code, nested HTML)
                    // means this paragraph is a real paragraph, not a figure.
                    return false;
            }
        }

        if (onlyImage is null)
        {
            return false;
        }

        var altText = ExtractPlainText(ConvertInlines(onlyImage));
        imageBlock = new MarkdownImageBlock(
            Url: NormalizeNullable(onlyImage.Url) ?? string.Empty,
            AltText: string.IsNullOrWhiteSpace(altText) ? null : altText,
            Title: NormalizeNullable(onlyImage.Title));
        return true;
    }

    private static string? ExtractAttr(Regex pattern, string tag)
    {
        var m = pattern.Match(tag);
        if (!m.Success)
        {
            return null;
        }

        for (var groupIndex = 1; groupIndex < m.Groups.Count; groupIndex++)
        {
            var group = m.Groups[groupIndex];
            if (!group.Success || string.IsNullOrWhiteSpace(group.Value))
            {
                continue;
            }

            return group.Value;
        }

        return null;
    }

    private static double? TryParseHtmlPixelDimension(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^2].TrimEnd();
        }

        if (normalized.Contains('%', StringComparison.Ordinal)
            || normalized.Contains("calc(", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("var(", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return double.TryParse(
                normalized,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var parsed)
            && parsed > 0
                ? parsed
                : null;
    }
}
