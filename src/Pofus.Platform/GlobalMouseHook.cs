using System.Runtime.InteropServices;
using Pofus.Core.Logging;

namespace Pofus.Platform;

/// <summary>Extra mouse buttons usable as a shortcut. Left and right are
/// deliberately absent: binding them would make the mouse unusable.</summary>
public enum MouseButton
{
    Middle = 0x04,  // VK_MBUTTON
    Extra1 = 0x05,  // VK_XBUTTON1 — "mouse4"
    Extra2 = 0x06,  // VK_XBUTTON2 — "mouse5"
}

/// <summary>
/// Watches the extra mouse buttons system-wide.
///
/// RegisterHotKey only knows about the keyboard, so mouse shortcuts need a
/// low-level hook instead. Windows calls this hook for every mouse event and
/// silently drops it if the callback dawdles (LowLevelHooksTimeout), so the
/// callback does the strict minimum: decide, then hand off. Nothing here waits
/// on anything.
/// </summary>
public sealed class GlobalMouseHook : IDisposable
{
    private const int WhMouseLl = 14;
    private const int HcAction = 0;
    private const int WmMButtonDown = 0x0207;
    private const int WmXButtonDown = 0x020B;

    private readonly IAppLogger _logger;

    // Kept in a field: the delegate is passed to unmanaged code, and letting it
    // be collected would tear the hook down at an unpredictable moment.
    private readonly Win32Native.LowLevelMouseProc _callback;
    private nint _hookHandle;

    /// <summary>
    /// Raised when a watched button goes down. Return true to swallow the
    /// event so the foreground application never sees it.
    /// </summary>
    public event Func<MouseButton, bool>? ButtonPressed;

    public GlobalMouseHook(IAppLogger logger)
    {
        _logger = logger;
        _callback = OnMouseEvent;
    }

    public bool IsInstalled => _hookHandle != nint.Zero;

    public void Install()
    {
        if (_hookHandle != nint.Zero)
        {
            return;
        }

        _hookHandle = Win32Native.SetWindowsHookEx(WhMouseLl, _callback, nint.Zero, 0);
        if (_hookHandle == nint.Zero)
        {
            // Reported, not swallowed: mouse shortcuts simply will not fire,
            // and the user deserves to know why (Principe I).
            _logger.LogError(
                $"Could not install the mouse hook (Win32 error {Marshal.GetLastWin32Error()}); "
                + "mouse-button shortcuts will not work.");
        }
    }

    public void Uninstall()
    {
        if (_hookHandle == nint.Zero)
        {
            return;
        }

        Win32Native.UnhookWindowsHookEx(_hookHandle);
        _hookHandle = nint.Zero;
    }

    private nint OnMouseEvent(int code, nint wParam, nint lParam)
    {
        if (code != HcAction)
        {
            return Win32Native.CallNextHookEx(_hookHandle, code, wParam, lParam);
        }

        var button = TryReadButton((int)wParam, lParam);
        if (button is not null && ButtonPressed?.Invoke(button.Value) == true)
        {
            // Returning non-zero stops the event here, so the game does not
            // also act on a button the user has bound to Pofus.
            return 1;
        }

        return Win32Native.CallNextHookEx(_hookHandle, code, wParam, lParam);
    }

    private static MouseButton? TryReadButton(int message, nint lParam)
    {
        if (message == WmMButtonDown)
        {
            return MouseButton.Middle;
        }

        if (message != WmXButtonDown)
        {
            return null;
        }

        // Which X button it was lives in the high word of mouseData.
        var data = Marshal.ReadInt32(lParam, Win32Native.MouseHookStructMouseDataOffset);
        return (data >> 16) switch
        {
            1 => MouseButton.Extra1,
            2 => MouseButton.Extra2,
            _ => null,
        };
    }

    public void Dispose() => Uninstall();
}
