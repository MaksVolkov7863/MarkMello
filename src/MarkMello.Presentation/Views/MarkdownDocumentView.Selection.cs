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


public sealed partial class MarkdownDocumentView
{
    public void SelectAll()
    {
        if (_textMap.Text.Length == 0)
        {
            ClearSelection();
            return;
        }

        SelectionAnchor = 0;
        SelectionStart = 0;
        SelectionEnd = _textMap.Text.Length;
        ApplySelectionToFragments();
    }

    public void ClearSelection()
    {
        SelectionAnchor = null;
        SelectionStart = 0;
        SelectionEnd = 0;
        ApplySelectionToFragments();
    }

    public void SelectRange(DocumentTextRange range)
    {
        if (_textMap.Text.Length == 0 || range.IsEmpty)
        {
            ClearSelection();
            return;
        }

        var start = Math.Clamp(range.Start, 0, _textMap.Text.Length);
        var end = Math.Clamp(range.End, start, _textMap.Text.Length);
        if (end <= start)
        {
            ClearSelection();
            return;
        }

        SelectionAnchor = start;
        SelectionStart = start;
        SelectionEnd = end;
        ApplySelectionToFragments();
    }

    private void SetSelection(int firstOffset, int secondOffset)
    {
        var range = DocumentTextRange.FromBounds(firstOffset, secondOffset);
        SelectionStart = range.Start;
        SelectionEnd = range.End;
        ApplySelectionToFragments();
    }

    private void ApplySelectionToFragments()
    {
        var range = new DocumentTextRange(SelectionStart, SelectionEnd);
        foreach (var fragment in _selectionFragments)
        {
            fragment.SelectionRange = range;
        }
    }

    private void CommitSelection(DocumentTextRange range, bool preserveOnRelease)
    {
        if (range.IsEmpty)
        {
            ClearSelection();
            return;
        }

        SelectionAnchor = range.Start;
        SelectionStart = range.Start;
        SelectionEnd = range.End;
        _preserveSelectionOnRelease = preserveOnRelease;
        ApplySelectionToFragments();
    }

    private void BeginPointerSession(
        PointerPressedEventArgs e,
        MarkdownDocumentSelectionFragmentBase fragment,
        Point localPosition,
        bool allowLinkActivation)
    {
        _isPointerPressed = true;
        _isDraggingSelection = false;
        _pointerPressOrigin = e.GetPosition(this);
        _pressedFragment = fragment;
        _pressedLink = allowLinkActivation && fragment.TryGetLinkAt(localPosition, out var pressedLink)
            ? pressedLink
            : null;
        e.Pointer.Capture(this);
    }

    private int ResolveDocumentOffset(Point position)
    {
        if (!TryResolveFragment(position, out var fragment, out var localPoint))
        {
            return 0;
        }

        return fragment.GetDocumentOffset(localPoint);
    }

    private bool TryResolveFragment(
        Point position,
        out MarkdownDocumentSelectionFragmentBase fragment,
        out Point localPoint)
    {
        fragment = null!;
        localPoint = default;

        if (_selectionFragments.Count == 0)
        {
            return false;
        }

        var fragments = new List<MarkdownDocumentSelectionFragmentBase>(_selectionFragments.Count);
        var candidates = new List<MarkdownFragmentHitTestCandidate>(_selectionFragments.Count);

        foreach (var candidateFragment in _selectionFragments)
        {
            var translated = this.TranslatePoint(position, candidateFragment);
            if (translated is null)
            {
                continue;
            }

            fragments.Add(candidateFragment);
            candidates.Add(new MarkdownFragmentHitTestCandidate(
                new Rect(0, 0, Math.Max(candidateFragment.Bounds.Width, 1), Math.Max(candidateFragment.Bounds.Height, 1)),
                translated.Value));
        }

        var bestIndex = MarkdownFragmentHitTester.FindBestIndex(candidates);
        if (bestIndex < 0)
        {
            return false;
        }

        fragment = fragments[bestIndex];
        localPoint = ClampPointToFragment(fragment, candidates[bestIndex].LocalPoint);
        return true;
    }

    private bool TryResolveLinkAtDocumentPoint(Point documentPoint, out MarkdownLinkSpan link)
    {
        link = default;
        if (!TryResolveFragment(documentPoint, out var fragment, out var localPoint))
        {
            return false;
        }

        return fragment.TryGetLinkAt(localPoint, out link);
    }

    private static Point ClampPointToFragment(MarkdownDocumentSelectionFragmentBase fragment, Point point)
    {
        var width = Math.Max(fragment.Bounds.Width, 1);
        var height = Math.Max(fragment.Bounds.Height, 1);
        return new Point(
            Math.Clamp(point.X, 0, width),
            Math.Clamp(point.Y, 0, height - 1));
    }

    internal static bool IsPointerInputFromScrollBarChrome(object? source)
    {
        if (source is not Control control)
        {
            return false;
        }

        return control is ScrollBar || control.FindAncestorOfType<ScrollBar>() is not null;
    }

    private static bool IsPointerInputFromCodeCopyButton(object? source)
    {
        if (source is not Control control)
        {
            return false;
        }

        return control.Classes.Contains("mm-code-copy-button")
            || control.FindAncestorOfType<Button>()?.Classes.Contains("mm-code-copy-button") == true;
    }

    private static bool HasCommandModifier(KeyModifiers modifiers)
        => modifiers.HasFlag(KeyModifiers.Control) || modifiers.HasFlag(KeyModifiers.Meta);

    private static Cursor? TryCreateCursor(StandardCursorType cursorType)
    {
        try
        {
            return new Cursor(cursorType);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private void ResetPointerState()
    {
        _isPointerPressed = false;
        _isDraggingSelection = false;
        _preserveSelectionOnRelease = false;
        _pointerPressOrigin = default;
        _pressedFragment = null;
        _pressedLink = null;
    }
}
