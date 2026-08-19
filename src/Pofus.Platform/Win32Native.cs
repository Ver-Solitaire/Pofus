using System.Runtime.InteropServices;
using System.Text;

namespace Pofus.Platform;

public static class Win32Native
{
    public delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    public delegate void WinEventDelegate(
        nint hWinEventHook, uint eventType, nint hwnd,
        int idObject, int idChild, uint eventThread, uint eventTime);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc enumProc, nint lParam);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(
        nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    public static extern nint SetWinEventHook(
        uint eventMin, uint eventMax, nint hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    public static extern bool UnhookWinEvent(nint hWinEventHook);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(nint hWnd, int id);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    public static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool IsIconic(nint hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool AllowSetForegroundWindow(uint dwProcessId);

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint dwExtraInfo);

    /// <summary>
    /// Timeout variant so a frozen game window can never hang the HUD's UI
    /// thread while we ask it for its icon (Principe II).
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint SendMessageTimeout(
        nint hWnd, uint msg, nint wParam, nint lParam, uint flags, uint timeoutMs, out nint result);

    [DllImport("user32.dll", EntryPoint = "GetClassLongPtrW", SetLastError = true)]
    public static extern nint GetClassLongPtr(nint hWnd, int nIndex);
}
