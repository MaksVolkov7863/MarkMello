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
    private void RebuildHeadingAnchorIndex()
    {
        _headingAnchorTargets.Clear();
        _headingAnchorCounts.Clear();

        foreach (var (block, control) in _headingAnchorRegistrations)
        {
            var baseAnchor = MarkdownHeadingAnchorSlugger.CreateAnchor(block.Inlines);
            if (string.IsNullOrEmpty(baseAnchor))
            {
                continue;
            }

            var count = _headingAnchorCounts.TryGetValue(baseAnchor, out var currentCount)
                ? currentCount
                : 0;
            _headingAnchorCounts[baseAnchor] = count + 1;

            var anchor = count == 0
                ? baseAnchor
                : string.Create(CultureInfo.InvariantCulture, $"{baseAnchor}-{count}");

            _headingAnchorTargets.TryAdd(anchor, control);
        }
    }

    private async Task TryActivatePressedLinkAsync(PointerReleasedEventArgs e)
    {
        if (_pressedFragment is null)
        {
            return;
        }

        var releasePosition = e.GetPosition(_pressedFragment);
        MarkdownLinkSpan? releasedLink = _pressedFragment.TryGetLinkAt(releasePosition, out var hitLink)
            ? hitLink
            : null;

        if (!MarkdownLinkActivationPolicy.CanActivateLink(
                _isDraggingSelection,
                SelectionAnchor,
                SelectionStart,
                SelectionEnd,
                _pressedLink,
                releasedLink))
        {
            return;
        }

        var pressedLink = _pressedLink!.Value;

        if (TryScrollToHeadingAnchor(pressedLink.Url))
        {
            return;
        }

        if (MarkdownLocalFileLinkResolver.TryResolve(pressedLink.Url, Document?.BaseDirectory, out var targetPath))
        {
            MarkdownFileLinkRequested?.Invoke(
                this,
                new MarkdownFileLinkRequestedEventArgs(pressedLink.Url, targetPath));
            return;
        }

        if (!Uri.TryCreate(pressedLink.Url, UriKind.Absolute, out var uri))
        {
            return;
        }

        var launcher = TopLevel.GetTopLevel(this)?.Launcher;
        if (launcher is null)
        {
            return;
        }

        await launcher.LaunchUriAsync(uri);
    }

    private void RegisterHeadingAnchor(MarkdownHeadingBlock block, Control headingControl)
        => _headingAnchorRegistrations.Add((block, headingControl));

    internal bool HasHeadingAnchor(string linkTarget)
        => MarkdownHeadingAnchorSlugger.TryNormalizeFragment(linkTarget, out var anchor)
            && _headingAnchorTargets.ContainsKey(anchor);

    private bool TryScrollToHeadingAnchor(string linkTarget)
    {
        if (!MarkdownHeadingAnchorSlugger.TryNormalizeFragment(linkTarget, out var anchor)
            || !_headingAnchorTargets.TryGetValue(anchor, out var target))
        {
            return false;
        }

        return TryScrollTargetIntoView(target);
    }

    private bool TryScrollTargetIntoView(Control target)
    {
        var scrollViewer = this.FindAncestorOfType<ScrollViewer>();
        if (scrollViewer is null)
        {
            return false;
        }

        var targetPoint = target.TranslatePoint(new Point(0, 0), scrollViewer);
        if (targetPoint is null)
        {
            return false;
        }

        const double topInset = 24;
        var nextOffsetY = Math.Clamp(
            scrollViewer.Offset.Y + targetPoint.Value.Y - topInset,
            0,
            scrollViewer.ScrollBarMaximum.Y);

        scrollViewer.Offset = new Vector(scrollViewer.Offset.X, nextOffsetY);
        return true;
    }
}
