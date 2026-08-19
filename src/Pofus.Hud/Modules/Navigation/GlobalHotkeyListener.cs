using System.Windows;
using System.Windows.Interop;
using Pofus.Core.Logging;
using Pofus.Core.Navigation;
using Pofus.Platform;

namespace Pofus.Hud.Modules.Navigation;

/// <summary>
/// Registers <c>RegisterHotKey</c> bindings against an existing WPF window's
/// real HWND (here, the always-alive HudWindow — no dedicated message-only
/// window needed) and raises <see cref="HotkeyPressed"/> on WM_HOTKEY,
/// regardless of which window currently has focus (FR-009 of feature 003).
///
/// Actions are identified by an opaque string id so this can carry both the
/// navigation actions ("nav:*") and the per-module un-hide actions
/// ("module:*") of feature 005 without their RegisterHotKey ids colliding —
/// ids are handed out by an internal counter rather than derived from the
/// action, which no string-hash scheme could guarantee (research.md #2).
/// This type stays agnostic of those prefixes; they are a caller convention.
/// </summary>
public sealed class GlobalHotkeyListener : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModNoRepeat = 0x4000;

    private readonly IWin32WindowApi _api;
    private readonly IAppLogger _logger;
    private readonly nint _windowHandle;
    private readonly HwndSource _hwndSource;
    private readonly Dictionary<int, string> _actionsById = [];
    private readonly Dictionary<string, int> _idsByAction = new(StringComparer.Ordinal);
    private readonly Dictionary<string, KeyCombo> _combosByAction = new(StringComparer.Ordinal);
    private int _nextHotkeyId;
    private bool _suspended;

    public event Action<string>? HotkeyPressed;

    /// <summary>
    /// <paramref name="hostWindow"/> must already be Loaded (have a real HWND)
    /// when this is constructed.
    /// </summary>
    public GlobalHotkeyListener(Window hostWindow, IWin32WindowApi api, IAppLogger logger)
    {
        _api = api;
        _logger = logger;
        _windowHandle = new WindowInteropHelper(hostWindow).Handle;
        _hwndSource = HwndSource.FromHwnd(_windowHandle)
            ?? throw new InvalidOperationException(
                "GlobalHotkeyListener requires the host window to be Loaded first.");
        _hwndSource.AddHook(WndProc);
    }

    /// <summary>Registers (or re-registers, replacing any previous binding) an action's hotkey.</summary>
    public void RegisterOrReplace(string actionId, KeyCombo combo)
    {
        _combosByAction[actionId] = combo;
        if (_suspended)
        {
            return; // ResumeAll() will register it
        }

        var id = GetOrCreateHotkeyId(actionId);
        _api.UnregisterHotKey(_windowHandle, id); // harmless no-op if not currently registered

        var modifiers = ToWin32Modifiers(combo.Modifiers) | ModNoRepeat;
        if (!_api.RegisterHotKey(_windowHandle, id, modifiers, combo.VirtualKeyCode))
        {
            var error = _api.GetLastWin32Error();
            _logger.LogWarning(
                $"RegisterHotKey failed for {actionId} ({combo.ToDisplayString()}), Win32 error {error} " +
                "(likely already claimed system-wide by another application).");
            _actionsById.Remove(id);
            return;
        }

        _actionsById[id] = actionId;
    }

    /// <summary>Removes an action's hotkey, if one is currently registered.</summary>
    public void Unregister(string actionId)
    {
        _combosByAction.Remove(actionId);
        if (!_idsByAction.TryGetValue(actionId, out var id))
        {
            return;
        }

        _api.UnregisterHotKey(_windowHandle, id);
        _actionsById.Remove(id);
    }

    /// <summary>
    /// Releases every hotkey so a shortcut-capture UI can actually observe the
    /// keys the user presses. Without this, a combination that is already
    /// registered is swallowed by Windows and delivered as WM_HOTKEY instead
    /// of reaching the focused window — the capture would hang and the bound
    /// action would fire instead, making conflicts impossible to report.
    /// Always pair with <see cref="ResumeAll"/>.
    /// </summary>
    public void SuspendAll()
    {
        if (_suspended)
        {
            return;
        }

        _suspended = true;
        foreach (var id in _actionsById.Keys)
        {
            _api.UnregisterHotKey(_windowHandle, id);
        }

        _actionsById.Clear();
    }

    /// <summary>Re-registers everything released by <see cref="SuspendAll"/>.</summary>
    public void ResumeAll()
    {
        if (!_suspended)
        {
            return;
        }

        _suspended = false;
        foreach (var (actionId, combo) in _combosByAction)
        {
            RegisterOrReplace(actionId, combo);
        }
    }

    /// <summary>
    /// Hotkey ids must be unique per process and stable for a given action, so
    /// that re-registering an action replaces its own binding rather than
    /// leaking a new one each time.
    /// </summary>
    private int GetOrCreateHotkeyId(string actionId)
    {
        if (_idsByAction.TryGetValue(actionId, out var existing))
        {
            return existing;
        }

        var id = _nextHotkeyId++;
        _idsByAction[actionId] = id;
        return id;
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WmHotkey && _actionsById.TryGetValue((int)wParam, out var actionId))
        {
            handled = true;
            HotkeyPressed?.Invoke(actionId);
        }

        return nint.Zero;
    }

    private static uint ToWin32Modifiers(KeyModifiers modifiers)
    {
        uint result = 0;
        if (modifiers.HasFlag(KeyModifiers.Alt))
        {
            result |= 0x0001;
        }

        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            result |= 0x0002;
        }

        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            result |= 0x0004;
        }

        if (modifiers.HasFlag(KeyModifiers.Win))
        {
            result |= 0x0008;
        }

        return result;
    }

    public void Dispose()
    {
        foreach (var id in _actionsById.Keys)
        {
            _api.UnregisterHotKey(_windowHandle, id);
        }

        _actionsById.Clear();
        _idsByAction.Clear();
        _combosByAction.Clear();
        _hwndSource.RemoveHook(WndProc);
    }
}
