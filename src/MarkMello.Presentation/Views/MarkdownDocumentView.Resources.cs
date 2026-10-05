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
    private IBrush? LookupBrush(string resourceKey)
        => this.TryFindResource(resourceKey, ActualThemeVariant, out var value) && value is IBrush brush
            ? brush
            : null;

    private static string GetLocalizedString(string key, string fallback)
    {
        if (Avalonia.Application.Current?.Resources.TryGetResource("Localization", null, out var resource) == true
            && resource is ILocalizationService localization)
        {
            var value = localization[key];
            return string.IsNullOrWhiteSpace(value) || value.StartsWith("[[", StringComparison.Ordinal)
                ? fallback
                : value;
        }

        return fallback;
    }
}
