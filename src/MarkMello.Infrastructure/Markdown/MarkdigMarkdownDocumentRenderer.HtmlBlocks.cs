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
    // --- HTML handling ----------------------------------------------------

    private static readonly Regex ImgTagPattern = new(
        @"<img\b[^>]*/?>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AltAttrPattern = new(
        @"\balt\s*=\s*(?:""([^""]*)""|'([^']*)')",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SrcAttrPattern = new(
        @"\bsrc\s*=\s*(?:""([^""]*)""|'([^']*)')",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TitleAttrPattern = new(
        @"\btitle\s*=\s*(?:""([^""]*)""|'([^']*)')",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex WidthAttrPattern = new(
        @"\bwidth\s*=\s*(?:""([^""]*)""|'([^']*)'|([^\s""'>/]+))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex HeightAttrPattern = new(
        @"\bheight\s*=\s*(?:""([^""]*)""|'([^']*)'|([^\s""'>/]+))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex LineBreakTagPattern = new(
        @"^<br\b[^>]*/?>$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AnyTagPattern = new(
        @"<[^>]+>",
        RegexOptions.Compiled);

    private static readonly Regex WhitespacePattern = new(
        @"\s+",
        RegexOptions.Compiled);

    // Patterns that must strip their *entire* body, not just the opening/closing
    // tags. If we only stripped <script> without its content we would leak
    // executable code text into the reading view.
    private static readonly Regex ScriptBodyPattern = new(
        @"<script\b[^>]*>.*?</script\s*>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex StyleBodyPattern = new(
        @"<style\b[^>]*>.*?</style\s*>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex CommentBodyPattern = new(
        @"<!--.*?-->",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex CDataBodyPattern = new(
        @"<!\[CDATA\[.*?\]\]>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex ProcessingInstructionPattern = new(
        @"<\?.*?\?>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex DoctypePattern = new(
        @"<!DOCTYPE\b[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Splits an HTML block body into a sequence of document blocks. Each
    /// &lt;img&gt; in the source becomes its own <see cref="MarkdownImageBlock"/>;
    /// the surrounding HTML fragments (with container tags like &lt;p&gt;,
    /// &lt;div&gt;, &lt;picture&gt; stripped and entities decoded) become
    /// paragraphs when they carry any visible text. This lets common README
    /// patterns -- "figure with caption", "centered picture", etc. -- render
    /// as image + caption rather than a literal "[image: alt] caption" line.
    /// </summary>
    private static void AppendHtmlBlock(string html, List<MarkdownBlock> target, MarkdownSourceSpan? sourceSpan)
    {
        if (string.IsNullOrEmpty(html))
        {
            return;
        }

        // Drop things whose *content* must not appear in the viewer at all,
        // not just their surrounding tags: script bodies, style rules,
        // comments, CDATA, processing instructions, doctype. Doing this by
        // content (not by Markdig's HtmlBlockType enum) keeps the code
        // independent of Markdig version-specific enum names.
        var scrubbed = html;
        scrubbed = ScriptBodyPattern.Replace(scrubbed, string.Empty);
        scrubbed = StyleBodyPattern.Replace(scrubbed, string.Empty);
        scrubbed = CommentBodyPattern.Replace(scrubbed, string.Empty);
        scrubbed = CDataBodyPattern.Replace(scrubbed, string.Empty);
        scrubbed = ProcessingInstructionPattern.Replace(scrubbed, string.Empty);
        scrubbed = DoctypePattern.Replace(scrubbed, string.Empty);

        var imgMatches = ImgTagPattern.Matches(scrubbed);
        if (imgMatches.Count == 0)
        {
            AppendHtmlTextParagraph(scrubbed, target, sourceSpan);
            return;
        }

        var cursor = 0;
        foreach (Match match in imgMatches)
        {
            AppendHtmlTextParagraph(scrubbed[cursor..match.Index], target, sourceSpan);

            if (TryBuildImageBlockFromImgTag(match.Value, out var imageBlock))
            {
                target.Add(WithSourceSpan(imageBlock, sourceSpan));
            }
            else
            {
                // Malformed <img> (no src) -- keep the alt-text placeholder
                // so the author still sees that something was intended here.
                target.Add(WithSourceSpan(
                    new MarkdownParagraphBlock([
                        new MarkdownTextInline(FormatImagePlaceholder(match.Value))
                    ]),
                    sourceSpan));
            }

            cursor = match.Index + match.Length;
        }

        AppendHtmlTextParagraph(scrubbed[cursor..], target, sourceSpan);
    }

    private static void AppendHtmlTextParagraph(string htmlFragment, List<MarkdownBlock> target, MarkdownSourceSpan? sourceSpan)
    {
        if (string.IsNullOrEmpty(htmlFragment))
        {
            return;
        }

        // Strip every remaining tag (<picture>, <source>, <div>, <p>, <br>, ...),
        // decode HTML entities, collapse whitespace.
        var text = AnyTagPattern.Replace(htmlFragment, " ");
        text = WebUtility.HtmlDecode(text);
        text = WhitespacePattern.Replace(text, " ").Trim();

        if (text.Length == 0)
        {
            return;
        }

        target.Add(WithSourceSpan(
            new MarkdownParagraphBlock([new MarkdownTextInline(text)]),
            sourceSpan));
    }

    private static bool TryBuildImageBlockFromImgTag(string imgTag, out MarkdownImageBlock imageBlock)
    {
        imageBlock = null!;
        var src = ExtractAttr(SrcAttrPattern, imgTag);
        if (string.IsNullOrWhiteSpace(src))
        {
            return false;
        }

        var alt = ExtractAttr(AltAttrPattern, imgTag);
        var title = ExtractAttr(TitleAttrPattern, imgTag);
        var width = TryParseHtmlPixelDimension(ExtractAttr(WidthAttrPattern, imgTag));
        var height = TryParseHtmlPixelDimension(ExtractAttr(HeightAttrPattern, imgTag));
        imageBlock = new MarkdownImageBlock(
            Url: src,
            AltText: string.IsNullOrWhiteSpace(alt) ? null : alt,
            Title: string.IsNullOrWhiteSpace(title) ? null : title,
            Width: width,
            Height: height);
        return true;
    }
}
