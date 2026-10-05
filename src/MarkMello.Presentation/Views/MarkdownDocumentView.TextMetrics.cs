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
    private double GetBodyLineHeight() => Math.Max(ReadingPreferences.FontSize * ReadingPreferences.LineHeight, ReadingPreferences.FontSize + 4);

    private double GetHeadingFontSize(int level)
    {
        var baseSize = ReadingPreferences.FontSize;
        return level switch
        {
            1 => baseSize * 2.1,
            2 => baseSize * 1.5,
            3 => baseSize * 1.2,
            4 => baseSize * 1.05,
            5 => baseSize * 0.95,
            6 => baseSize * 0.95,
            _ => baseSize
        };
    }
}
