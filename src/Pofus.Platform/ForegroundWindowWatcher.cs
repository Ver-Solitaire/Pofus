using Pofus.Core.Logging;

namespace Pofus.Platform;

/// <summary>
/// Raises <see cref="ForegroundWindowChanged"/> whenever any window (game or
/// otherwise) becomes the foreground window — not filtered to Dofus — so the HUD
/// can reassert its topmost position regardless of which window just took focus
/// (research.md § Maintien au premier plan au-dessus de TOUTES les fenêtres Dofus).
/// Uses SetWinEventHook instead of polling to stay reactive without spending CPU.
/// </summary>
public sealed class ForegroundWindowWatcher : IDisposable
{
    private const uint EventSystemForeground = 0x0003;
    private const uint WineventOutofcontext = 0x0000;

    private readonly IWin32WindowApi _api;
    private readonly IAppLogger _logger;
    private readonly Win32Native.WinEventDelegate _callback;
    private nint _hookHandle;

    public event Action<nint>? ForegroundWindowChanged;

    public ForegroundWindowWatcher(IAppLogger logger)
        : this(logger, new Win32WindowApi())
    {
    }

    public ForegroundWindowWatcher(IAppLogger logger, IWin32WindowApi api)
    {
        _logger = logger;
        _api = api;
        // Stored in a field so the delegate is not garbage-collected while the
        // unmanaged hook still holds a reference to it.
        _callback = OnWinEvent;
    }

    public void Start()
    {
        if (_hookHandle != nint.Zero)
        {
            return;
        }

        _hookHandle = _api.SetWinEventHook(
            EventSystemForeground, EventSystemForeground, _callback, 0, 0, WineventOutofcontext);

        if (_hookHandle == nint.Zero)
        {
            _logger.LogError(
                "SetWinEventHook(EVENT_SYSTEM_FOREGROUND) failed; the HUD will not " +
                "automatically reassert itself above newly focused windows.");
        }
    }

    private void OnWinEvent(
        nint hWinEventHook, uint eventType, nint hwnd,
        int idObject, int idChild, uint eventThread, uint eventTime)
    {
        if (hwnd == nint.Zero)
        {
            return;
        }

        ForegroundWindowChanged?.Invoke(hwnd);
    }

    public void Dispose()
    {
        if (_hookHandle != nint.Zero)
        {
            _api.UnhookWinEvent(_hookHandle);
            _hookHandle = nint.Zero;
        }
    }
}
