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
    private async Task CopySelectionToClipboardAsync()
    {
        await CopyTextToClipboardAsync(SelectedText).ConfigureAwait(true);
    }

    private async Task CopyTextToClipboardAsync(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            return;
        }

        await ClipboardExtensions.SetTextAsync(
            clipboard,
            text.Replace("\n", Environment.NewLine, StringComparison.Ordinal));
    }

    private ContextMenu BuildContextMenu()
    {
        _copyMenuItem = new MenuItem
        {
            Header = GetLocalizedString("ContextCopy", "Copy"),
            InputGesture = new KeyGesture(Key.C, KeyModifiers.Control)
        };
        _copyMenuItem.Click += OnCopyMenuItemClick;

        _copyLinkMenuItem = new MenuItem
        {
            Header = GetLocalizedString("ContextCopyLink", "Copy link")
        };
        _copyLinkMenuItem.Click += OnCopyLinkMenuItemClick;

        _copyTelegramMarkdownMenuItem = new MenuItem
        {
            Header = GetLocalizedString("ContextCopyTelegramMarkdown", "Copy selection as Telegram Markdown")
        };
        _copyTelegramMarkdownMenuItem.Click += OnCopyTelegramMarkdownMenuItemClick;

        _selectAllMenuItem = new MenuItem
        {
            Header = GetLocalizedString("ContextSelectAll", "Select all"),
            InputGesture = new KeyGesture(Key.A, KeyModifiers.Control)
        };
        _selectAllMenuItem.Click += OnSelectAllMenuItemClick;

        var menu = new ContextMenu();
        menu.Items.Add(_copyMenuItem);
        menu.Items.Add(_copyLinkMenuItem);
        menu.Items.Add(_copyTelegramMarkdownMenuItem);
        menu.Items.Add(_selectAllMenuItem);
        menu.Opening += OnContextMenuOpening;
        menu.Closed += (_, _) =>
        {
            _contextMenuLink = null;
            _contextMenuSelectedLinkUrls = Array.Empty<string>();
        };
        return menu;
    }

    private void OnContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _contextMenuSelectedLinkUrls = GetSelectedLinkUrls();
        UpdateContextMenuHeaders();

        // Enable Copy only when there is a selection.
        // Enable Select All only when there is any text to select.
        if (_copyMenuItem is not null)
        {
            _copyMenuItem.IsEnabled = HasSelection;
        }

        if (_copyLinkMenuItem is not null)
        {
            _copyLinkMenuItem.IsEnabled = _contextMenuSelectedLinkUrls.Count > 0 || _contextMenuLink.HasValue;
        }

        if (_copyTelegramMarkdownMenuItem is not null)
        {
            _copyTelegramMarkdownMenuItem.IsEnabled = HasSelection && Document is { Blocks.Count: > 0 };
        }

        if (_selectAllMenuItem is not null)
        {
            _selectAllMenuItem.IsEnabled = _textMap.Text.Length > 0;
        }
    }

    private void UpdateContextMenuHeaders()
    {
        if (_copyMenuItem is not null)
        {
            _copyMenuItem.Header = GetLocalizedString("ContextCopy", "Copy");
        }

        if (_copyLinkMenuItem is not null)
        {
            _copyLinkMenuItem.Header = _contextMenuSelectedLinkUrls.Count > 1
                ? GetLocalizedString("ContextCopyLinks", "Copy links")
                : GetLocalizedString("ContextCopyLink", "Copy link");
        }

        if (_copyTelegramMarkdownMenuItem is not null)
        {
            _copyTelegramMarkdownMenuItem.Header = GetLocalizedString(
                "ContextCopyTelegramMarkdown",
                "Copy selection as Telegram Markdown");
        }

        if (_selectAllMenuItem is not null)
        {
            _selectAllMenuItem.Header = GetLocalizedString("ContextSelectAll", "Select all");
        }
    }

    private async void OnCopyMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (!HasSelection)
        {
            return;
        }

        await CopySelectionToClipboardAsync();
    }

    private void OnSelectAllMenuItemClick(object? sender, RoutedEventArgs e)
    {
        Focus(NavigationMethod.Pointer);
        SelectAll();
    }

    private async void OnCopyLinkMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (_contextMenuSelectedLinkUrls.Count > 1)
        {
            await CopyTextToClipboardAsync(string.Join("\n", _contextMenuSelectedLinkUrls)).ConfigureAwait(true);
            return;
        }

        if (_contextMenuLink is { } link)
        {
            await CopyTextToClipboardAsync(link.Url).ConfigureAwait(true);
            return;
        }

        if (_contextMenuSelectedLinkUrls.Count == 1)
        {
            await CopyTextToClipboardAsync(_contextMenuSelectedLinkUrls[0]).ConfigureAwait(true);
        }
    }

    private IReadOnlyList<string> GetSelectedLinkUrls()
    {
        if (!HasSelection || Document is not { Blocks.Count: > 0 } document)
        {
            return Array.Empty<string>();
        }

        return TelegramMarkdownFormatter.GetSelectionLinkUrls(
            document,
            new DocumentTextRange(SelectionStart, SelectionEnd));
    }

    private async void OnCopyTelegramMarkdownMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (!HasSelection || Document is not { Blocks.Count: > 0 } document)
        {
            return;
        }

        var selectionRange = new DocumentTextRange(SelectionStart, SelectionEnd);
        var markdown = TelegramMarkdownFormatter.FormatSelection(document, selectionRange);
        var html = TelegramMarkdownFormatter.FormatSelectionHtml(document, selectionRange);
        await CopyTelegramMarkdownToClipboardAsync(markdown, html).ConfigureAwait(true);
    }

    private async Task CopyTelegramMarkdownToClipboardAsync(string markdown, string htmlFragment)
    {
        if (string.IsNullOrEmpty(markdown) && string.IsNullOrEmpty(htmlFragment))
        {
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            return;
        }

        var item = new DataTransferItem();
        if (!string.IsNullOrEmpty(markdown))
        {
            item.SetText(markdown.Replace("\n", Environment.NewLine, StringComparison.Ordinal));
        }

        if (!string.IsNullOrEmpty(htmlFragment))
        {
            item.Set(HtmlClipboardFormat, Encoding.UTF8.GetBytes(CreateHtmlClipboardDocument(htmlFragment)));
            item.Set(WindowsHtmlClipboardFormat, CreateWindowsHtmlClipboardPayload(htmlFragment));
        }

        var dataTransfer = new DataTransfer();
        dataTransfer.Add(item);
        await clipboard.SetDataAsync(dataTransfer).ConfigureAwait(true);
        await clipboard.FlushAsync().ConfigureAwait(true);
    }

    private static string CreateHtmlClipboardDocument(string htmlFragment)
        => "<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head><body>" + htmlFragment + "</body></html>";

    private static byte[] CreateWindowsHtmlClipboardPayload(string htmlFragment)
    {
        const string startFragmentMarker = "<!--StartFragment-->";
        const string endFragmentMarker = "<!--EndFragment-->";
        const string headerTemplate = "Version:0.9\r\nStartHTML:0000000000\r\nEndHTML:0000000000\r\nStartFragment:0000000000\r\nEndFragment:0000000000\r\n";

        var htmlPrefix = "<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head><body>" + startFragmentMarker;
        var htmlSuffix = endFragmentMarker + "</body></html>";
        var html = htmlPrefix + htmlFragment + htmlSuffix;

        var startHtml = Encoding.UTF8.GetByteCount(headerTemplate);
        var startFragment = startHtml + Encoding.UTF8.GetByteCount(htmlPrefix);
        var endFragment = startFragment + Encoding.UTF8.GetByteCount(htmlFragment);
        var endHtml = startHtml + Encoding.UTF8.GetByteCount(html);

        var header = string.Create(
            CultureInfo.InvariantCulture,
            $"Version:0.9\r\nStartHTML:{startHtml:D10}\r\nEndHTML:{endHtml:D10}\r\nStartFragment:{startFragment:D10}\r\nEndFragment:{endFragment:D10}\r\n");

        return Encoding.UTF8.GetBytes(header + html);
    }
}
