using System.Windows;
using System.Windows.Input;
using Pofus.Core.Logging;
using Pofus.Core.Models;

namespace Pofus.Hud;

/// <summary>
/// Moving a borderless Pofus window, and keeping it reachable.
///
/// Every Pofus window is <c>WindowStyle=None</c>, so there is no title bar and
/// dragging goes through <see cref="Window.DragMove"/>. That method throws
/// <see cref="InvalidOperationException"/> the moment the primary button is not
/// down — which happens more often than it looks: WPF re-raises MouseDown as
/// MouseLeftButtonDown through the event route, and the button can already have
/// been released by the time our handler runs (a fast click, or a click that
/// stole the foreground and moved input elsewhere). That crashed Pofus.
/// </summary>
public static class WindowPlacement
{
    /// <summary>
    /// Set once by the composition root. Static because these helpers are used
    /// by windows that carry no logger of their own.
    /// </summary>
    public static IAppLogger? Logger { get; set; }

    /// <summary>
    /// Starts a window drag, but only if the left button really is down.
    /// Safe to wire to any MouseLeftButtonDown handler.
    /// </summary>
    public static void BeginDrag(Window window, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        // Mouse.LeftButton is the live device state; e.ButtonState only says what
        // the message reported when it was queued.
        if (Mouse.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        try
        {
            window.DragMove();
        }
        catch (InvalidOperationException ex)
        {
            // The checks above are not atomic: Windows can deliver WM_LBUTTONUP
            // between them and the call. Rare, harmless, and never a reason to
            // take the whole HUD down — but it is recorded (Principe I).
            Logger?.LogWarning(
                $"Déplacement de « {window.Title} » abandonné : le bouton a été relâché " +
                $"pendant l'appel ({ex.Message}).");
        }
    }

    /// <summary>
    /// Brings a persisted position back inside the desktop, measuring the actual
    /// virtual screen and delegating the arithmetic to
    /// <see cref="WindowPositionGuard"/>.
    /// </summary>
    public static (double Left, double Top) ClampToDesktop(double left, double top)
    {
        var clamped = WindowPositionGuard.Clamp(
            left,
            top,
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);

        if (clamped.Left != left || clamped.Top != top)
        {
            Logger?.LogInfo(
                $"Position enregistrée ({left}; {top}) hors du bureau — ramenée à " +
                $"({clamped.Left}; {clamped.Top}).");
        }

        return clamped;
    }
}
