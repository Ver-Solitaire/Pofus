using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Pofus.Hud;

/// <summary>
/// Gives a borderless, transparent Pofus window native resizing on every edge
/// and corner.
///
/// Windows normally draws and hit-tests resize borders itself, but a window with
/// <c>WindowStyle=None</c> and <c>AllowsTransparency=True</c> has no non-client
/// area, so it never reports an edge. Answering WM_NCHITTEST ourselves restores
/// exactly the standard behaviour — including the diagonal corner cursors and
/// the snap gestures — without drawing any chrome.
/// </summary>
public static class WindowResizer
{
    private const int WmNcHitTest = 0x0084;

    // Standard HT* results; returning them makes Windows do the resize for us.
    private const int HtLeft = 10;
    private const int HtRight = 11;
    private const int HtTop = 12;
    private const int HtTopLeft = 13;
    private const int HtTopRight = 14;
    private const int HtBottom = 15;
    private const int HtBottomLeft = 16;
    private const int HtBottomRight = 17;

    /// <summary>
    /// How far from an edge still counts as a grab. Generous on purpose: these
    /// windows keep a transparent margin for their drop shadow, so the visible
    /// panel border sits inside the window rectangle.
    /// </summary>
    private const double GrabThickness = 18;

    public static void Attach(Window window)
    {
        if (window.IsLoaded)
        {
            Hook(window);
            return;
        }

        window.Loaded += (_, _) => Hook(window);
    }

    private static void Hook(Window window)
    {
        var source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
        source?.AddHook((nint hwnd, int msg, nint wParam, nint lParam, ref bool handled) =>
        {
            if (msg != WmNcHitTest || window.ResizeMode == ResizeMode.NoResize)
            {
                return nint.Zero;
            }

            var hit = HitTest(window, lParam);
            if (hit == 0)
            {
                return nint.Zero; // not an edge: let WPF handle it normally
            }

            handled = true;
            return hit;
        });
    }

    private static int HitTest(Window window, nint lParam)
    {
        // lParam packs screen coordinates as two signed 16-bit values.
        var screenX = (short)((long)lParam & 0xFFFF);
        var screenY = (short)(((long)lParam >> 16) & 0xFFFF);

        Point local;
        try
        {
            local = window.PointFromScreen(new Point(screenX, screenY));
        }
        catch (InvalidOperationException)
        {
            return 0; // window not yet connected to a presentation source
        }

        var onLeft = local.X <= GrabThickness;
        var onRight = local.X >= window.ActualWidth - GrabThickness;
        var onTop = local.Y <= GrabThickness;
        var onBottom = local.Y >= window.ActualHeight - GrabThickness;

        // Corners first, so diagonal resizing wins over the edges it overlaps.
        return (onLeft, onRight, onTop, onBottom) switch
        {
            (true, _, true, _) => HtTopLeft,
            (_, true, true, _) => HtTopRight,
            (true, _, _, true) => HtBottomLeft,
            (_, true, _, true) => HtBottomRight,
            (true, _, _, _) => HtLeft,
            (_, true, _, _) => HtRight,
            (_, _, true, _) => HtTop,
            (_, _, _, true) => HtBottom,
            _ => 0,
        };
    }
}
