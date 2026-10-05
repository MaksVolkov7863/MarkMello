using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using MarkMello.Domain;

namespace MarkMello.Presentation.Views.Markdown;

internal sealed class MarkdownAlertBlockView : Grid
{
    private readonly MarkdownAlertKind _kind;
    private readonly Control _title;
    private readonly Border _surface;
    private readonly Border _bar;
    private readonly Canvas _icon;

    public MarkdownAlertBlockView(MarkdownAlertKind kind, Control title, Control body)
    {
        _kind = kind;
        _title = title;
        Classes.Add("mm-md-alert");
        Margin = new Thickness(0, 0, 0, 20);

        _surface = new Border { CornerRadius = new CornerRadius(0, 6, 6, 0) };
        _bar = new Border
        {
            Width = 4,
            CornerRadius = new CornerRadius(2),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        _icon = MarkdownAlertIcon.Create(kind);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("16,8,*"), MinHeight = 21 };
        header.Children.Add(new Viewbox
        {
            Width = 16, Height = 16, Child = _icon,
            VerticalAlignment = VerticalAlignment.Center,
        });
        Grid.SetColumn(title, 2);
        header.Children.Add(title);

        var content = new StackPanel
        {
            Margin = new Thickness(22, 12, 16, 14),
            Spacing = 6,
        };
        content.Children.Add(header);
        content.Children.Add(body);
        Children.Add(_surface);
        Children.Add(content);
        Children.Add(_bar);

        AttachedToVisualTree += (_, _) => UpdateBrushes();
        ActualThemeVariantChanged += (_, _) => UpdateBrushes();
        ResourcesChanged += (_, _) => UpdateBrushes();
    }

    private void UpdateBrushes()
    {
        _surface.Background = FindBrush("MmSurfaceBrush");
        var accent = FindBrush($"MmAlert{_kind}Brush");
        _bar.Background = accent;
        foreach (var shape in _icon.Children.OfType<Shape>())
        {
            shape.Stroke = accent;
        }

        switch (_title)
        {
            case MarkdownSelectionTextFragment fragment:
                fragment.BaseForeground = accent;
                break;
            case TextBlock text:
                text.Foreground = accent;
                break;
        }
    }

    private IBrush? FindBrush(string key)
        => this.TryFindResource(key, ActualThemeVariant, out var value) ? value as IBrush : null;
}
