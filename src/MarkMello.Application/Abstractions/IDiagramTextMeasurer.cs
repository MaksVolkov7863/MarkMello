namespace MarkMello.Application.Abstractions;

/// <summary>Native text metrics used to lay out diagram labels before producing SVG.</summary>
public interface IDiagramTextMeasurer
{
    DiagramTextSize Measure(string text, string fontFamily, double fontSize);
}

public readonly record struct DiagramTextSize(double Width, double Height);
