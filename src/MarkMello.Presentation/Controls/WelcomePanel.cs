using Avalonia;
using Avalonia.Controls;

namespace MarkMello.Presentation.Controls;

/// <summary>Keeps the centered welcome content clear of the bottom support button.</summary>
public sealed class WelcomePanel : Grid
{
    protected override Size ArrangeOverride(Size arrangeSize)
    {
        var arrangedSize = base.ArrangeOverride(arrangeSize);
        if (Children.Count != 2)
        {
            return arrangedSize;
        }

        var content = Children[0];
        var bounds = content.Bounds;
        var overlap = bounds.Bottom - Children[1].Bounds.Top;
        if (overlap > 0)
        {
            content.Arrange(new Rect(bounds.X, bounds.Y - overlap, bounds.Width, bounds.Height));
        }

        return arrangedSize;
    }
}
