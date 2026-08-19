namespace Pofus.Platform;

/// <summary>
/// Thin, mockable wrapper around the raw user32.dll P/Invoke calls in
/// <see cref="Win32Native"/>. Exists so <c>Pofus.Platform</c> services stay
/// unit-testable without a real Windows desktop session (Principle III).
/// </summary>
public interface IWin32WindowApi
{
    bool EnumWindows(Win32Native.EnumWindowsProc enumProc);

    uint GetWindowThreadProcessId(nint hWnd, out uint processId);

    bool IsWindowVisible(nint hWnd);

    string GetWindowText(nint hWnd);

    bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

    nint SetWinEventHook(
        uint eventMin, uint eventMax, Win32Native.WinEventDelegate callback,
        uint idProcess, uint idThread, uint flags);

    bool UnhookWinEvent(nint hookHandle);

    int GetLastWin32Error();

    bool RegisterHotKey(nint hWnd, int id, uint modifiers, uint virtualKeyCode);

    bool UnregisterHotKey(nint hWnd, int id);

    bool SetForegroundWindow(nint hWnd);

    nint GetForegroundWindow();

    bool IsIconic(nint hWnd);

    /// <summary>Restores a minimized window (SW_RESTORE).</summary>
    bool RestoreWindow(nint hWnd);

    bool AllowSetForegroundWindow(uint processId);

    /// <summary>
    /// Simulates a tap of the Alt key. Windows relaxes the SetForegroundWindow
    /// restriction when the last input event was a keypress — the same trick
    /// the reference project uses in focus_window (research.md).
    /// </summary>
    void SimulateAltKeyTap();

    /// <summary>
    /// The icon a window shows in the taskbar, or <see cref="nint.Zero"/> if it
    /// has none. Dofus sets this per character (it is the class icon), unlike
    /// the window-class icon which every window of the process shares.
    /// The returned HICON is owned by the target window — never destroy it.
    /// </summary>
    nint GetWindowIcon(nint hWnd);
}
