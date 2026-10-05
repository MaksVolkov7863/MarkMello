using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using MarkMello.Domain;
using ShapePath = Avalonia.Controls.Shapes.Path;

namespace MarkMello.Presentation.Views.Markdown;

/// <summary>Lucide geometries used by the approved HTML alert preview.</summary>
internal static class MarkdownAlertIcon
{
    public static Canvas Create(MarkdownAlertKind kind)
    {
        var canvas = new Canvas { Width = 24, Height = 24, IsHitTestVisible = false };
        if (kind == MarkdownAlertKind.Note)
        {
            AddPath(canvas, new EllipseGeometry(new Rect(2, 2, 20, 20)));
        }

        foreach (var path in GetPaths(kind))
        {
            AddPath(canvas, StreamGeometry.Parse(path));
        }
        return canvas;
    }

    private static void AddPath(Canvas canvas, Geometry geometry)
        => canvas.Children.Add(new ShapePath
        {
            Data = geometry,
            StrokeThickness = 1.7,
            StrokeLineCap = PenLineCap.Round,
            StrokeJoin = PenLineJoin.Round,
        });

    private static string[] GetPaths(MarkdownAlertKind kind) => kind switch
    {
        MarkdownAlertKind.Note => ["M12 16v-4", "M12 8h.01"],
        MarkdownAlertKind.Tip =>
        [
            "M15 14c.2-1 .7-1.7 1.5-2.5 1-.9 1.5-2.2 1.5-3.5A6 6 0 0 0 6 8c0 1 .2 2.2 1.5 3.5.7.7 1.3 1.5 1.5 2.5",
            "M9 18h6", "M10 22h4",
        ],
        MarkdownAlertKind.Important =>
        [
            "M22 17a2 2 0 0 1-2 2H6.828a2 2 0 0 0-1.414.586l-2.202 2.202A.71.71 0 0 1 2 21.286V5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2z",
            "M12 15h.01", "M12 7v4",
        ],
        MarkdownAlertKind.Warning =>
        [
            "m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3",
            "M12 9v4", "M12 17h.01",
        ],
        MarkdownAlertKind.Caution =>
        [
            "M12 16h.01", "M12 8v4",
            "M15.312 2a2 2 0 0 1 1.414.586l4.688 4.688A2 2 0 0 1 22 8.688v6.624a2 2 0 0 1-.586 1.414l-4.688 4.688a2 2 0 0 1-1.414.586H8.688a2 2 0 0 1-1.414-.586l-4.688-4.688A2 2 0 0 1 2 15.312V8.688a2 2 0 0 1 .586-1.414l4.688-4.688A2 2 0 0 1 8.688 2z",
        ],
        _ => [],
    };
}
