using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace MarkMello.Presentation.Controls;

/// <summary>Fades only text that extends past its arranged viewport.</summary>
public sealed class OverflowFadeTextBlock : TextBlock
{
    private static readonly LinearGradientBrush FadeMask = new()
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
        GradientStops =
        [
            new GradientStop(Colors.White, 0),
            new GradientStop(Colors.White, 0.82),
            new GradientStop(Colors.Transparent, 1)
        ]
    };

    protected override Type StyleKeyOverride => typeof(TextBlock);

    protected override Size ArrangeOverride(Size finalSize)
    {
        var result = base.ArrangeOverride(finalSize);
        OpacityMask = TextLayout.WidthIncludingTrailingWhitespace > finalSize.Width
            ? FadeMask
            : null;
        return result;
    }
}
