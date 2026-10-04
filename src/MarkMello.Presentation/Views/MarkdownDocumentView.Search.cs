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
    /// Currently active search query, or null when find is not active.
    /// </summary>
    public string? ActiveSearchQuery => _activeSearchQuery.Length == 0 ? null : _activeSearchQuery;

    /// <summary>
    /// Total number of matches for the active query (0 when not searching).
    /// </summary>
    public int MatchCount => _searchMatches.Count;

    /// <summary>
    /// Zero-based index of the current match, or -1 when there are no matches.
    /// </summary>
    public int MatchIndex => _searchMatches.Count == 0 ? -1 : _activeMatchIndex;

    /// <summary>
    /// Raised whenever the match set or the current match changes, including
    /// after document rebuilds that re-apply an active query.
    /// </summary>
    public event EventHandler? SearchStateChanged;

    /// <summary>
    /// Starts (or updates) a document-wide, case-insensitive search over the
    /// rendered text. An empty or null query clears all search highlights.
    /// </summary>
    public void ApplySearchQuery(string? query)
    {
        // Not trimmed: a query is whatever the user typed, spaces included.
        // Trimming would make " the " and a bare space unsearchable.
        var normalized = query ?? string.Empty;
        if (string.Equals(_activeSearchQuery, normalized, StringComparison.Ordinal))
        {
            return;
        }

        _activeSearchQuery = normalized;
        RebuildSearchMatches(keepCurrentIndex: false);
        TryScrollToActiveMatch();
    }

    public bool FindNext()
    {
        if (_searchMatches.Count == 0)
        {
            return false;
        }

        _activeMatchIndex = MarkdownTextSearch.NextIndex(_activeMatchIndex, _searchMatches.Count);
        ApplyActiveSearchHighlightToFragments();
        SearchStateChanged?.Invoke(this, EventArgs.Empty);
        TryScrollToActiveMatch();
        return true;
    }

    public bool FindPrevious()
    {
        if (_searchMatches.Count == 0)
        {
            return false;
        }

        _activeMatchIndex = MarkdownTextSearch.PreviousIndex(_activeMatchIndex, _searchMatches.Count);
        ApplyActiveSearchHighlightToFragments();
        SearchStateChanged?.Invoke(this, EventArgs.Empty);
        TryScrollToActiveMatch();
        return true;
    }

    /// <summary>
    /// Scrolls the current match into view. Called after rebuilds so the
    /// active result stays visible when the document re-renders.
    /// </summary>
    public void ScrollToActiveMatch() => TryScrollToActiveMatch();

    private void RebuildSearchMatches(bool keepCurrentIndex)
    {
        _searchMatches.Clear();
        if (_activeSearchQuery.Length > 0)
        {
            _searchMatches.AddRange(MarkdownTextSearch.FindAll(_textMap.Text, _activeSearchQuery));
        }

        if (_searchMatches.Count == 0)
        {
            _activeMatchIndex = -1;
        }
        else
        {
            _activeMatchIndex = keepCurrentIndex && _activeMatchIndex >= 0 && _activeMatchIndex < _searchMatches.Count
                ? _activeMatchIndex
                : 0;
        }

        ApplySearchHighlightsToFragments();
        SearchStateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Раскладывает совпадения по фрагментам.
    ///
    /// Совпадения приходят из <see cref="MarkdownTextSearch.FindAll"/>
    /// отсортированными и без перекрытий, поэтому фрагменту нужны не все они, а
    /// только окно: находим бинарным поиском первое дотягивающееся до фрагмента
    /// и идём вперёд, пока совпадения начинаются раньше его конца. Перебор всех
    /// совпадений для каждого фрагмента на документе в сотню килобайт — это
    /// порядка миллиона проверок на каждое нажатие в поле поиска.
    /// </summary>
    private void ApplySearchHighlightsToFragments()
    {
        foreach (var fragment in _selectionFragments)
        {
            fragment.SearchHighlightRanges = CollectMatchesWithin(fragment.DocumentRange);
        }

        ApplyActiveSearchHighlightToFragments();
    }

    /// <summary>
    /// Обновляет только активное совпадение. Переход к следующему или
    /// предыдущему результату не меняет набор совпадений, поэтому пересобирать
    /// списки диапазонов ради одной подсветки незачем.
    /// </summary>
    private void ApplyActiveSearchHighlightToFragments()
    {
        var activeRange = _activeMatchIndex >= 0 && _activeMatchIndex < _searchMatches.Count
            ? _searchMatches[_activeMatchIndex]
            : (DocumentTextRange?)null;

        foreach (var fragment in _selectionFragments)
        {
            if (activeRange is not { } active)
            {
                fragment.ActiveSearchHighlight = null;
                continue;
            }

            var intersection = fragment.DocumentRange.Intersection(active);
            fragment.ActiveSearchHighlight = intersection.IsEmpty ? null : intersection;
        }
    }

    private IReadOnlyList<DocumentTextRange> CollectMatchesWithin(DocumentTextRange fragmentRange)
    {
        List<DocumentTextRange>? ranges = null;

        for (var index = FindFirstMatchReaching(fragmentRange.Start); index < _searchMatches.Count; index++)
        {
            var match = _searchMatches[index];
            if (match.Start >= fragmentRange.End)
            {
                break;
            }

            var intersection = fragmentRange.Intersection(match);
            if (!intersection.IsEmpty)
            {
                // Большинство фрагментов не содержит ни одного совпадения —
                // список заводим только когда есть что в него положить.
                ranges ??= [];
                ranges.Add(intersection);
            }
        }

        return ranges ?? (IReadOnlyList<DocumentTextRange>)Array.Empty<DocumentTextRange>();
    }

    /// <summary>
    /// Индекс первого совпадения, которое заканчивается после
    /// <paramref name="offset"/>. Совпадения отсортированы по началу и не
    /// перекрываются, поэтому конец монотонен и бинарный поиск корректен.
    /// </summary>
    private int FindFirstMatchReaching(int offset)
    {
        var low = 0;
        var high = _searchMatches.Count;

        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            if (_searchMatches[middle].End <= offset)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    private void TryScrollToActiveMatch()
    {
        if (_activeMatchIndex < 0 || _activeMatchIndex >= _searchMatches.Count)
        {
            return;
        }

        var match = _searchMatches[_activeMatchIndex];
        var fragment = FindFragmentForDocumentOffset(match.Start);
        if (fragment is null)
        {
            return;
        }

        var scrollViewer = this.FindAncestorOfType<ScrollViewer>();
        if (scrollViewer is null)
        {
            return;
        }

        var yInFragment = 0.0;
        if (fragment is MarkdownSelectionTextFragment textFragment
            && textFragment.TryGetLineTopForLocalOffset(match.Start - fragment.DocumentRange.Start, out var lineY))
        {
            yInFragment = lineY;
        }

        var targetPoint = fragment.TranslatePoint(new Point(0, yInFragment), scrollViewer);
        if (targetPoint is null)
        {
            return;
        }

        const double topInset = 24;
        var nextOffsetY = Math.Clamp(
            scrollViewer.Offset.Y + targetPoint.Value.Y - topInset,
            0,
            scrollViewer.ScrollBarMaximum.Y);

        scrollViewer.Offset = new Vector(scrollViewer.Offset.X, nextOffsetY);
    }

    private MarkdownDocumentSelectionFragmentBase? FindFragmentForDocumentOffset(int offset)
    {
        foreach (var fragment in _selectionFragments)
        {
            if (offset >= fragment.DocumentRange.Start && offset < fragment.DocumentRange.End)
            {
                return fragment;
            }
        }

        return null;
    }
}
