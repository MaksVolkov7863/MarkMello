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
    /// <summary>
    /// Вертикальное смещение в документе для дробной позиции в исходнике
    /// (номер строки плюс доля продвижения внутри неё).
    ///
    /// Позиция дробная не для красоты: абзац в markdown обычно записан одной
    /// длинной строкой, поэтому целочисленный номер строки не различает начало
    /// и конец абзаца и preview застревал бы на его верхней кромке, пока
    /// редактор прокручивает весь абзац.
    /// </summary>
    internal bool TryGetVerticalOffsetForSourcePosition(double sourcePosition, out double offsetY)
    {
        offsetY = 0;
        var map = CreateSourcePositionMap();
        if (map.Count == 0)
        {
            return false;
        }

        if (sourcePosition <= map[0].SourceLine)
        {
            offsetY = map[0].Y;
            return true;
        }

        for (var index = 1; index < map.Count; index++)
        {
            var next = map[index];
            if (sourcePosition > next.SourceLine)
            {
                continue;
            }

            var previous = map[index - 1];
            var ratio = (sourcePosition - previous.SourceLine) / (next.SourceLine - previous.SourceLine);
            offsetY = previous.Y + ((next.Y - previous.Y) * ratio);
            return true;
        }

        offsetY = map[^1].Y;
        return true;
    }

    /// <summary>
    /// Обратное отображение: дробная позиция в исходнике для вертикального
    /// смещения в документе.
    /// </summary>
    internal bool TryGetSourcePositionForVerticalOffset(double offsetY, out double sourcePosition)
    {
        sourcePosition = 0;
        var map = CreateSourcePositionMap();
        if (map.Count == 0)
        {
            return false;
        }

        var normalizedOffset = Math.Max(0, offsetY);
        if (normalizedOffset <= map[0].Y)
        {
            sourcePosition = map[0].SourceLine;
            return true;
        }

        for (var index = 1; index < map.Count; index++)
        {
            var next = map[index];
            if (normalizedOffset > next.Y)
            {
                continue;
            }

            var previous = map[index - 1];
            var ratio = (normalizedOffset - previous.Y) / (next.Y - previous.Y);
            sourcePosition = previous.SourceLine + ((next.SourceLine - previous.SourceLine) * ratio);
            return true;
        }

        sourcePosition = map[^1].SourceLine;
        return true;
    }

    /// <summary>
    /// Монотонная кусочно-линейная карта «строка исходника → Y документа».
    ///
    /// Собирается из измеренных якорей блоков; вложенные блоки (пункты списка,
    /// абзацы цитаты) дают дополнительные точки и тем самым разрешение внутри
    /// крупных блоков. Точки, не возрастающие сразу по обеим координатам,
    /// отбрасываются — иначе интерполяция делила бы на ноль или ехала назад.
    /// </summary>
    private List<MarkdownSourcePositionPoint> CreateSourcePositionMap()
    {
        var anchors = CreateMeasuredSourceLineAnchors();
        var map = new List<MarkdownSourcePositionPoint>(anchors.Count + 1);

        foreach (var anchor in anchors)
        {
            if (map.Count == 0)
            {
                map.Add(new MarkdownSourcePositionPoint(anchor.StartLine, anchor.Y));
                continue;
            }

            var last = map[^1];
            if (anchor.StartLine > last.SourceLine && anchor.Y > last.Y)
            {
                map.Add(new MarkdownSourcePositionPoint(anchor.StartLine, anchor.Y));
            }
        }

        if (map.Count == 0)
        {
            return map;
        }

        // Замыкающая точка: конец последнего блока в низу документа, иначе
        // хвост документа не имел бы куда отображаться. Берём максимальную
        // конечную строку, а не последний по Y якорь: им может оказаться
        // вложенный блок с более коротким span.
        var lastLine = 0;
        foreach (var anchor in anchors)
        {
            lastLine = Math.Max(lastLine, anchor.EndLine);
        }

        var documentBottom = _root.TranslatePoint(new Point(0, _root.Bounds.Height), this)?.Y;
        var tailLine = lastLine + 1;
        if (documentBottom is { } bottom && tailLine > map[^1].SourceLine && bottom > map[^1].Y)
        {
            map.Add(new MarkdownSourcePositionPoint(tailLine, bottom));
        }

        return map;
    }

    // Test-only: enumerates registered source-line anchor spans without
    // requiring the view to be laid out. Edit-mode scroll synchronization
    // needs every block with a SourceSpan (including diagrams) to register
    // here so the editor cursor can map to the preview block. Layout-
    // measured offsets live behind <see cref="CreateMeasuredSourceLineAnchors"/>.
    internal IReadOnlyList<MarkdownSourceSpan> EnumerateRegisteredSourceSpans()
    {
        var spans = new List<MarkdownSourceSpan>(_sourceLineAnchors.Count);
        foreach (var anchor in _sourceLineAnchors)
        {
            spans.Add(anchor.SourceSpan);
        }
        return spans;
    }

    private List<MarkdownSourceLineAnchorSnapshot> CreateMeasuredSourceLineAnchors()
    {
        var result = new List<MarkdownSourceLineAnchorSnapshot>(_sourceLineAnchors.Count);

        foreach (var anchor in _sourceLineAnchors)
        {
            if (anchor.Control.Bounds.Width <= 0 || anchor.Control.Bounds.Height <= 0)
            {
                continue;
            }

            var origin = anchor.Control.TranslatePoint(new Point(0, 0), this);
            if (origin is null)
            {
                continue;
            }

            result.Add(new MarkdownSourceLineAnchorSnapshot(
                anchor.SourceSpan.StartLine,
                anchor.SourceSpan.EndLine,
                Math.Max(0, origin.Value.Y)));
        }

        result.Sort(static (left, right) =>
        {
            var visualComparison = left.Y.CompareTo(right.Y);
            return visualComparison != 0
                ? visualComparison
                : left.StartLine.CompareTo(right.StartLine);
        });

        return result;
    }
}
