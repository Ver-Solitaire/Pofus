using System.Runtime.InteropServices;
using System.Text;

namespace Pofus.Platform;

/// <summary>Real implementation of <see cref="IWin32WindowApi"/>, calling user32.dll.</summary>
public sealed class Win32WindowApi : IWin32WindowApi
{
    public bool EnumWindows(Win32Native.EnumWindowsProc enumProc) =>
        Win32Native.EnumWindows(enumProc, nint.Zero);

    public uint GetWindowThreadProcessId(nint hWnd, out uint processId) =>
        Win32Native.GetWindowThreadProcessId(hWnd, out processId);

    public bool IsWindowVisible(nint hWnd) => Win32Native.IsWindowVisible(hWnd);

    public string GetWindowText(nint hWnd)
    {
        var length = Win32Native.GetWindowTextLength(hWnd);
        if (length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(length + 1);
        Win32Native.GetWindowText(hWnd, builder, builder.Capacity);
        return builder.ToString();
    }

    public bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint flags) =>
        Win32Native.SetWindowPos(hWnd, hWndInsertAfter, x, y, cx, cy, flags);

    public nint SetWinEventHook(
        uint eventMin, uint eventMax, Win32Native.WinEventDelegate callback,
        uint idProcess, uint idThread, uint flags) =>
        Win32Native.SetWinEventHook(eventMin, eventMax, nint.Zero, callback, idProcess, idThread, flags);

    public bool UnhookWinEvent(nint hookHandle) => Win32Native.UnhookWinEvent(hookHandle);

    public int GetLastWin32Error() => Marshal.GetLastWin32Error();

    private const int SwRestore = 9;
    private const byte VkMenu = 0x12; // VK_MENU (Alt)
    private const uint KeyeventfKeyup = 0x0002;

    public bool RegisterHotKey(nint hWnd, int id, uint modifiers, uint virtualKeyCode) =>
        Win32Native.RegisterHotKey(hWnd, id, modifiers, virtualKeyCode);

    public bool UnregisterHotKey(nint hWnd, int id) => Win32Native.UnregisterHotKey(hWnd, id);

    public bool SetForegroundWindow(nint hWnd) => Win32Native.SetForegroundWindow(hWnd);

    public nint GetForegroundWindow() => Win32Native.GetForegroundWindow();

    public bool IsIconic(nint hWnd) => Win32Native.IsIconic(hWnd);

    public bool RestoreWindow(nint hWnd) => Win32Native.ShowWindow(hWnd, SwRestore);

    public bool AllowSetForegroundWindow(uint processId) => Win32Native.AllowSetForegroundWindow(processId);

    public void SimulateAltKeyTap()
    {
        Win32Native.keybd_event(VkMenu, 0, 0, 0);
        Win32Native.keybd_event(VkMenu, 0, KeyeventfKeyup, 0);
    }

    private const uint WmGetIcon = 0x007F;
    private const uint SmtoAbortIfHung = 0x0002;
    private const uint GetIconTimeoutMs = 500;
    private const int GclpHicon = -14;
    private const int GclpHiconSm = -34;

    public nint GetWindowIcon(nint hWnd)
    {
        // ICON_SMALL2 first: it is the one Windows itself shows in the taskbar,
        // and the only one Dofus sets per character. ICON_BIG/ICON_SMALL are
        // fallbacks for windows that only set those.
        foreach (var iconType in (nint[])[2, 1, 0])
        {
            Win32Native.SendMessageTimeout(
                hWnd, WmGetIcon, iconType, nint.Zero, SmtoAbortIfHung, GetIconTimeoutMs, out var icon);
            if (icon != nint.Zero)
            {
                return icon;
            }
        }

        // Last resort: the window-class icon. Shared by every window of the
        // process, so it cannot tell two characters apart — only useful for
        // windows that set no per-window icon at all.
        var classIcon = Win32Native.GetClassLongPtr(hWnd, GclpHicon);
        return classIcon != nint.Zero ? classIcon : Win32Native.GetClassLongPtr(hWnd, GclpHiconSm);
    }
}
