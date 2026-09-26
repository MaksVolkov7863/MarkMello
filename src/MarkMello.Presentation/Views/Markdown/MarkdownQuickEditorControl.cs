using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace MarkMello.Presentation.Views.Markdown;

/// <summary>
/// Инлайн-редактор простого текста блока для режима просмотра без переключения в двухпанельный вид.
/// </summary>
public sealed class MarkdownQuickEditorControl : TextBox
{
    private bool _isCommitted;

    public event EventHandler? CommitRequested;
    public event EventHandler? CancelRequested;

    public MarkdownQuickEditorControl()
    {
        Classes.Add("mm-quick-inline-editor");
        AcceptsReturn = true;
        AcceptsTab = false;
        TextWrapping = TextWrapping.Wrap;
        UseLayoutRounding = true;
    }

    public void SetInitialCaret(int caretIndex)
    {
        var clamped = Math.Clamp(caretIndex, 0, Text?.Length ?? 0);
        CaretIndex = clamped;
        SelectionStart = clamped;
        SelectionEnd = clamped;
    }

    public void RequestCommit()
    {
        if (_isCommitted)
        {
            return;
        }

        _isCommitted = true;
        CommitRequested?.Invoke(this, EventArgs.Empty);
    }

    public void RequestCancel()
    {
        if (_isCommitted)
        {
            return;
        }

        _isCommitted = true;
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            RequestCommit();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && (e.KeyModifiers & KeyModifiers.Control) != 0)
        {
            RequestCommit();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.S && (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0)
        {
            RequestCommit();
            // Don't mark as handled so window KeyBinding Ctrl+S can trigger SaveCommand
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        RequestCommit();
    }
}
