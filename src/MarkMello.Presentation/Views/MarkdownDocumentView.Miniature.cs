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
    internal DocumentMiniatureSnapshot CreateMiniatureSnapshot()
    {
        if (Document is null || Bounds.Height <= 0 || Bounds.Width <= 0)
        {
            return DocumentMiniatureSnapshot.Empty;
        }

        return new DocumentMiniatureSnapshot(
            totalWidth: Math.Max(1, Bounds.Width),
            totalHeight: Math.Max(1, Bounds.Height));
    }

    internal void RenderMiniature(DrawingContext context, Rect targetBounds)
    {
        var snapshot = CreateMiniatureSnapshot();
        if (snapshot.IsEmpty || targetBounds.Width <= 0 || targetBounds.Height <= 0)
        {
            return;
        }

        var scaleX = targetBounds.Width / snapshot.TotalWidth;
        var scaleY = targetBounds.Height / snapshot.TotalHeight;

        using (context.PushClip(targetBounds))
        {
            foreach (var border in _root.GetVisualDescendants().OfType<Border>().Where(IsMiniatureStructuralBorder))
            {
                DrawBorderDecorationMiniature(context, border, targetBounds, scaleX, scaleY);
            }

            foreach (var fragment in _selectionFragments)
            {
                DrawControlMiniature(context, fragment, targetBounds, scaleX, scaleY);
            }

            foreach (var imageView in _root.GetVisualDescendants().OfType<MarkdownImageView>())
            {
                DrawImagePlaceholderMiniature(context, imageView, targetBounds, scaleX, scaleY);
            }

            foreach (var rule in _root.GetVisualDescendants().OfType<Border>().Where(static border => border.Classes.Contains("mm-md-hr")))
            {
                DrawHorizontalRuleMiniature(context, rule, targetBounds, scaleX, scaleY);
            }
        }
    }

    private void DrawControlMiniature(
        DrawingContext context,
        Control control,
        Rect targetBounds,
        double scaleX,
        double scaleY)
    {
        if (control.Bounds.Width <= 0 || control.Bounds.Height <= 0)
        {
            return;
        }

        var origin = control.TranslatePoint(new Point(0, 0), this);
        if (origin is null)
        {
            return;
        }

        var matrix = new Matrix(
            scaleX,
            0,
            0,
            scaleY,
            targetBounds.X + origin.Value.X * scaleX,
            targetBounds.Y + origin.Value.Y * scaleY);

        using (context.PushTransform(matrix))
        {
            if (control is MarkdownSelectionTextFragment textFragment)
            {
                textFragment.RenderMiniature(context);
                return;
            }

            control.Render(context);
        }
    }

    private void DrawBorderDecorationMiniature(
        DrawingContext context,
        Border border,
        Rect targetBounds,
        double scaleX,
        double scaleY)
    {
        var bounds = TranslateControlBounds(border);
        if (bounds is null)
        {
            return;
        }

        var target = MapMiniatureRect(bounds.Value, targetBounds, scaleX, scaleY);
        if (target.Width <= 0 || target.Height <= 0)
        {
            return;
        }

        var background = border.Background;
        var borderBrush = border.BorderBrush;
        var pen = borderBrush is null || IsEmptyThickness(border.BorderThickness)
            ? null
            : new Pen(borderBrush, 1);

        if (background is null && pen is null)
        {
            return;
        }

        context.DrawRectangle(background, pen, target, 1.5, 1.5);
    }

    private void DrawImagePlaceholderMiniature(
        DrawingContext context,
        Control imageView,
        Rect targetBounds,
        double scaleX,
        double scaleY)
    {
        var bounds = TranslateControlBounds(imageView);
        if (bounds is null)
        {
            return;
        }

        var target = MapMiniatureRect(bounds.Value, targetBounds, scaleX, scaleY);
        if (target.Width <= 0 || target.Height <= 0)
        {
            return;
        }

        var fill = LookupBrush("MmSurfaceRaisedBrush") ?? LookupBrush("MmCodeBackgroundBrush") ?? Brushes.Transparent;
        var stroke = LookupBrush("MmBorderSubtleBrush") ?? LookupBrush("MmTextFaintBrush");
        context.DrawRectangle(fill, stroke is null ? null : new Pen(stroke, 1), target, 1.5, 1.5);
    }

    private void DrawHorizontalRuleMiniature(
        DrawingContext context,
        Control rule,
        Rect targetBounds,
        double scaleX,
        double scaleY)
    {
        var bounds = TranslateControlBounds(rule);
        if (bounds is null)
        {
            return;
        }

        var target = MapMiniatureRect(bounds.Value, targetBounds, scaleX, scaleY);
        if (target.Width <= 0 || target.Height <= 0)
        {
            return;
        }

        var brush = LookupBrush("MmBorderBrush") ?? LookupBrush("MmTextFaintBrush") ?? Brushes.Gray;
        context.DrawRectangle(brush, null, target);
    }

    private Rect? TranslateControlBounds(Control control)
    {
        if (control.Bounds.Width <= 0 || control.Bounds.Height <= 0)
        {
            return null;
        }

        var origin = control.TranslatePoint(new Point(0, 0), this);
        return origin is null
            ? null
            : new Rect(origin.Value, control.Bounds.Size);
    }

    private static bool IsMiniatureStructuralBorder(Border border)
        => border.Classes.Contains("mm-md-quote")
            || border.Classes.Contains("mm-md-codeblock")
            || border.Classes.Contains("mm-md-table")
            || border.Classes.Contains("mm-md-table-header-cell")
            || border.Classes.Contains("mm-md-table-cell");

    private static bool IsEmptyThickness(Thickness thickness)
        => thickness.Left <= 0 && thickness.Top <= 0 && thickness.Right <= 0 && thickness.Bottom <= 0;

    private static Rect MapMiniatureRect(Rect sourceBounds, Rect targetBounds, double scaleX, double scaleY)
        => new(
            targetBounds.X + sourceBounds.X * scaleX,
            targetBounds.Y + sourceBounds.Y * scaleY,
            sourceBounds.Width * scaleX,
            Math.Max(1, sourceBounds.Height * scaleY));
}
