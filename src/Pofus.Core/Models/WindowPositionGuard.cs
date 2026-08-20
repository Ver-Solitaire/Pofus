namespace Pofus.Core.Models;

/// <summary>
/// Keeps a restored window position reachable.
///
/// Pofus windows are borderless and can only be moved by grabbing them, so a
/// position restored outside the desktop — saved on a monitor since unplugged,
/// or before a resolution change — leaves the window impossible to bring back.
/// Pure arithmetic, no WPF: the caller supplies the desktop bounds.
/// </summary>
public static class WindowPositionGuard
{
    /// <summary>How much of the window must stay inside the desktop to be grabbable.</summary>
    public const double DefaultMinimumVisibleExtent = 48;

    /// <summary>
    /// Returns <paramref name="left"/>/<paramref name="top"/> unchanged when they
    /// already leave enough of the window on the desktop, and the nearest
    /// acceptable position otherwise. A non-finite coordinate (a hand-edited or
    /// corrupt preferences file) falls back to the desktop's top-left corner.
    ///
    /// The bounds describe the whole virtual desktop as one rectangle, so on an
    /// L-shaped multi-monitor layout the result can still land on a gap between
    /// monitors. It guarantees "not lost off-screen", not "on a monitor".
    /// </summary>
    public static (double Left, double Top) Clamp(
        double left,
        double top,
        double boundsLeft,
        double boundsTop,
        double boundsWidth,
        double boundsHeight,
        double minimumVisibleExtent = DefaultMinimumVisibleExtent)
    {
        if (!double.IsFinite(left) || !double.IsFinite(top))
        {
            return (boundsLeft, boundsTop);
        }

        var maxLeft = Math.Max(boundsLeft, boundsLeft + boundsWidth - minimumVisibleExtent);
        var maxTop = Math.Max(boundsTop, boundsTop + boundsHeight - minimumVisibleExtent);

        return (Math.Clamp(left, boundsLeft, maxLeft), Math.Clamp(top, boundsTop, maxTop));
    }
}
