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
    private Control BuildTable(MarkdownTableBlock table, string path)
    {
        var columnCount = Math.Max(
            table.Header.Count,
            table.Rows.Count == 0 ? 0 : table.Rows.Max(static row => row.Count));

        if (columnCount == 0)
        {
            return BuildFallback(table);
        }

        var grid = new Grid
        {
            ColumnSpacing = 0,
            RowSpacing = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        }

        var totalRows = table.Rows.Count + (table.Header.Count > 0 ? 1 : 0);
        for (var rowIndex = 0; rowIndex < totalRows; rowIndex++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }

        // Design `.mm-table` switches to the sans stack at 0.92em of body.
        var sansFontFamily = LookupFontFamily("MmDocumentSansFontFamily");
        var bodyCellFontSize = ReadingPreferences.FontSize * 0.92;
        var headerCellFontSize = ReadingPreferences.FontSize * 0.85;

        // Index of the last *data* row (not the header). Used to suppress
        // the trailing bottom border so the table does not end on a line.
        var lastDataRowIndex = table.Rows.Count > 0 ? totalRows - 1 : -1;

        var currentRow = 0;
        if (table.Header.Count > 0)
        {
            AddTableRow(
                grid, table.Header, currentRow,
                isHeader: true, isLastDataRow: false,
                pathPrefix: $"{path}.h",
                fontFamily: sansFontFamily,
                headerFontSize: headerCellFontSize,
                bodyFontSize: bodyCellFontSize);
            currentRow++;
        }

        for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
        {
            AddTableRow(
                grid, table.Rows[rowIndex], currentRow,
                isHeader: false, isLastDataRow: currentRow == lastDataRowIndex,
                pathPrefix: $"{path}.r{rowIndex}.c",
                fontFamily: sansFontFamily,
                headerFontSize: headerCellFontSize,
                bodyFontSize: bodyCellFontSize);
            currentRow++;
        }

        return new Border
        {
            Classes = { "mm-md-table" },
            Child = grid,
            // Design `.mm-table` margin is 1.4em top and bottom.
            Margin = new Thickness(0, (int)(ReadingPreferences.FontSize * 1.4), 0, (int)(ReadingPreferences.FontSize * 1.4))
        };
    }

    private void AddTableRow(
        Grid grid,
        IReadOnlyList<MarkdownTableCell> cells,
        int rowIndex,
        bool isHeader,
        bool isLastDataRow,
        string pathPrefix,
        FontFamily fontFamily,
        double headerFontSize,
        double bodyFontSize)
    {
        for (var columnIndex = 0; columnIndex < grid.ColumnDefinitions.Count; columnIndex++)
        {
            var cell = columnIndex < cells.Count
                ? cells[columnIndex]
                : new MarkdownTableCell(Array.Empty<MarkdownInline>());

            Control content;
            if (isHeader)
            {
                // Design: 0.85em size, semibold, soft colour, 0.05em letter-spacing.
                // Note: design also specifies "text-transform: uppercase", which
                // Avalonia does not support without mutating the characters
                // themselves (and breaking copy semantics). We therefore keep
                // the original case and approximate the visual weight via
                // letter-spacing + soft colour + smaller size.
                content = BuildSelectionFragment(
                    $"{pathPrefix}{columnIndex}",
                    cell.Inlines,
                    margin: default,
                    fontSize: headerFontSize,
                    lineHeight: Math.Max(headerFontSize * 1.45, headerFontSize + 4),
                    fontWeight: FontWeight.SemiBold,
                    fontStyle: FontStyle.Normal,
                    fallbackClassName: "mm-md-table-header",
                    baseFontFamily: fontFamily,
                    baseForeground: LookupBrush("MmTextSoftBrush"),
                    letterSpacing: headerFontSize * 0.05);
            }
            else
            {
                content = BuildSelectionFragment(
                    $"{pathPrefix}{columnIndex}",
                    cell.Inlines,
                    margin: default,
                    fontSize: bodyFontSize,
                    lineHeight: Math.Max(bodyFontSize * 1.55, bodyFontSize + 4),
                    fontWeight: FontWeight.Normal,
                    fontStyle: FontStyle.Normal,
                    fallbackClassName: "mm-md-table-text",
                    baseFontFamily: fontFamily);
            }

            var border = new Border
            {
                Classes = { isHeader ? "mm-md-table-header-cell" : "mm-md-table-cell" },
                Child = content
            };

            if (!isHeader && isLastDataRow)
            {
                // Suppresses the border-bottom on the final row so the table
                // does not terminate on an orphan divider line.
                border.Classes.Add("mm-md-table-cell-last");
            }

            Grid.SetRow(border, rowIndex);
            Grid.SetColumn(border, columnIndex);
            grid.Children.Add(border);
        }
    }
}
