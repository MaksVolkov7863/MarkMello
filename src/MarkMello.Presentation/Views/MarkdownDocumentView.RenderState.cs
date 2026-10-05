using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MarkMello.Application.Abstractions;
using MarkMello.Domain;
using MarkMello.Presentation.Clipboard;
using MarkMello.Presentation.Localization;
using MarkMello.Presentation.Views.Markdown;
using MarkMello.Presentation.Views.Markdown.Minimap;
using System.Globalization;
using System.Text;
using System.Threading;

namespace MarkMello.Presentation.Views;


internal readonly record struct MarkdownSourceLineVisualAnchor(Control Control, MarkdownSourceSpan SourceSpan);

/// <summary>
/// Source-line anchor stored relative to its top-level block's start line, so a
/// reused block can be re-anchored after an edit shifted it up or down.
/// </summary>
internal readonly record struct BuiltSourceAnchor(Control Control, int RelativeStartLine, int RelativeEndLine);

/// <summary>
/// Everything one top-level block contributed to the last render, kept so the
/// block can be re-adopted verbatim when the next parse produces an equal block.
/// </summary>
internal sealed class BuiltTopLevelBlock
{
    public required MarkdownBlock Block { get; set; }

    public required Control Control { get; init; }

    public required MarkdownDocumentSelectionFragmentBase[] Fragments { get; init; }

    /// <summary>Fragment paths with the leading <c>b{index}</c> segment removed.</summary>
    public required string[] FragmentRelativePaths { get; init; }

    public required BuiltSourceAnchor[] SourceAnchors { get; init; }

    public required (MarkdownHeadingBlock Block, Control Control)[] HeadingAnchors { get; init; }
}

internal readonly record struct MarkdownSourceLineAnchorSnapshot(int StartLine, int EndLine, double Y);

/// <summary>
/// One knot of the monotone source-line ↔ document-offset map used by edit-mode
/// scroll synchronization.
/// </summary>
internal readonly record struct MarkdownSourcePositionPoint(double SourceLine, double Y);
