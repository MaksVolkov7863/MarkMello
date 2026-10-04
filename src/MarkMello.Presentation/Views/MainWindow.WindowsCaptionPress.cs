using Avalonia.Controls;

namespace MarkMello.Presentation.Views;

public partial class MainWindow
{
    private const uint WmNcLButtonDown = 0x00A1;
    private const int HtCaption = 2;

    /// <summary>
    /// On Windows the drag area of the title bar answers as the native caption, so a left
    /// press there goes to Windows (drag, double-click maximize) and never reaches the
    /// pointer handlers. Avalonia passes right presses on to the client, left ones it does
    /// not. The window procedure still sees the left press: closing open menus and panels
    /// there keeps the title bar dismissing them like the rest of the window does. The
    /// message is left unhandled, so Windows goes on with the press.
    /// </summary>
    private void AttachWindowsCaptionPressHook()
    {
        if (OperatingSystem.IsWindows())
        {
            Win32Properties.AddWndProcHookCallback(this, OnWindowsCaptionPress);
        }
    }

    private IntPtr OnWindowsCaptionPress(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmNcLButtonDown
            && wParam == HtCaption
            && CanCloseOverlayOnOutsidePress())
        {
            _viewModel.CloseOverlayCommand.Execute(null);
        }

        return IntPtr.Zero;
    }
}
