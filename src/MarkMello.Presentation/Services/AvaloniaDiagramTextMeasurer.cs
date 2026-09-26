using System.Globalization;
using Avalonia;
using Avalonia.Media;
using MarkMello.Application.Abstractions;

namespace MarkMello.Presentation.Services;

public sealed class AvaloniaDiagramTextMeasurer : IDiagramTextMeasurer
{
    public DiagramTextSize Measure(string text, string fontFamily, double fontSize)
    {
        // Match the first-family resolution used by AotSafeSvgImage's text drawable.
        var family = fontFamily.Split(',', StringSplitOptions.TrimEntries)[0].Trim('"', '\'');
        var formatted = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily(family)),
            fontSize,
            Brushes.Black);
        return new DiagramTextSize(formatted.WidthIncludingTrailingWhitespace, formatted.Height);
    }
}
