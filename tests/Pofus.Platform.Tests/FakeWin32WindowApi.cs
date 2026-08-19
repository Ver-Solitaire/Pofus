using Pofus.Platform;

namespace Pofus.Platform.Tests;

internal sealed class FakeWin32WindowApi : IWin32WindowApi
{
    public List<nint> WindowHandles { get; } = [];
    public Dictionary<nint, bool> Visibility { get; } = [];
    public Dictionary<nint, uint> ProcessIds { get; } = [];
    public Dictionary<nint, string> WindowTitles { get; } = [];
    public bool EnumWindowsSucceeds { get; set; } = true;
    public bool SetWindowPosSucceeds { get; set; } = true;
    public nint SetWinEventHookReturnValue { get; set; } = 1;
    public int LastWin32Error { get; set; } = 5;

    public List<(nint Handle, nint InsertAfter, uint Flags)> SetWindowPosCalls { get; } = [];

    public bool EnumWindows(Win32Native.EnumWindowsProc enumProc)
    {
        if (!EnumWindowsSucceeds)
        {
            return false;
        }

        foreach (var handle in WindowHandles)
        {
            if (!enumProc(handle, nint.Zero))
            {
                break;
            }
        }

        return true;
    }

    public uint GetWindowThreadProcessId(nint hWnd, out uint processId)
    {
        processId = ProcessIds.GetValueOrDefault(hWnd, 0u);
        return processId;
    }

    public bool IsWindowVisible(nint hWnd) => Visibility.GetValueOrDefault(hWnd, false);

    public string GetWindowText(nint hWnd) => WindowTitles.GetValueOrDefault(hWnd, string.Empty);

    public bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint flags)
    {
        SetWindowPosCalls.Add((hWnd, hWndInsertAfter, flags));
        return SetWindowPosSucceeds;
    }

    public nint SetWinEventHook(
        uint eventMin, uint eventMax, Win32Native.WinEventDelegate callback,
        uint idProcess, uint idThread, uint flags) => SetWinEventHookReturnValue;

    public bool UnhookWinEvent(nint hookHandle) => true;

    public int GetLastWin32Error() => LastWin32Error;

    public bool RegisterHotKeySucceeds { get; set; } = true;
    public bool IsIconicResult { get; set; }
    public bool SetForegroundWindowSucceeds { get; set; } = true;
    public nint ForegroundWindow { get; set; }
    public List<(nint Handle, int Id, uint Modifiers, uint VirtualKeyCode)> RegisterHotKeyCalls { get; } = [];
    public List<(nint Handle, int Id)> UnregisterHotKeyCalls { get; } = [];
    public int SimulateAltKeyTapCallCount { get; private set; }

    public bool RegisterHotKey(nint hWnd, int id, uint modifiers, uint virtualKeyCode)
    {
        RegisterHotKeyCalls.Add((hWnd, id, modifiers, virtualKeyCode));
        return RegisterHotKeySucceeds;
    }

    public bool UnregisterHotKey(nint hWnd, int id)
    {
        UnregisterHotKeyCalls.Add((hWnd, id));
        return true;
    }

    public bool SetForegroundWindow(nint hWnd) => SetForegroundWindowSucceeds;

    public nint GetForegroundWindow() => ForegroundWindow;

    public bool IsIconic(nint hWnd) => IsIconicResult;

    public bool RestoreWindow(nint hWnd) => true;

    public bool AllowSetForegroundWindow(uint processId) => true;

    public void SimulateAltKeyTap() => SimulateAltKeyTapCallCount++;

    /// <summary>Icon handle returned per window; absent means "no icon".</summary>
    public Dictionary<nint, nint> IconsByWindow { get; } = [];

    public nint GetWindowIcon(nint hWnd) => IconsByWindow.GetValueOrDefault(hWnd);
}
