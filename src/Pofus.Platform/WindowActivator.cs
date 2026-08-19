using Pofus.Core.Logging;

namespace Pofus.Platform;

public interface IWindowActivator
{
    /// <summary>
    /// Brings the given window to the foreground, working around Windows'
    /// SetForegroundWindow restriction (research.md). Never throws — returns
    /// false and logs on failure rather than leaving the caller guessing.
    /// Blocks the calling thread for up to ~300ms while confirming the
    /// activation — callers on the UI thread (e.g. a WM_HOTKEY handler)
    /// MUST invoke this off-thread (e.g. via Task.Run) to respect
    /// Principe II (no blocking the UI thread).
    /// </summary>
    bool TryActivate(nint windowHandle);
}

/// <summary>
/// Ports the reference project's focus_window "unlock" sequence: restore if
/// minimized, AllowSetForegroundWindow, simulate an Alt key tap (Windows
/// relaxes the restriction right after a keypress), then SetForegroundWindow
/// and poll briefly to confirm. Unlike the reference implementation's bare
/// `except: pass`, failure here is detected and logged explicitly (Principe I).
/// </summary>
public sealed class WindowActivator : IWindowActivator
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(30);
    private const int MaxPollAttempts = 10; // ~300ms total, matching the reference project

    private readonly IWin32WindowApi _api;
    private readonly IAppLogger _logger;

    public WindowActivator(IAppLogger logger)
        : this(logger, new Win32WindowApi())
    {
    }

    public WindowActivator(IAppLogger logger, IWin32WindowApi api)
    {
        _logger = logger;
        _api = api;
    }

    public bool TryActivate(nint windowHandle)
    {
        if (windowHandle == nint.Zero)
        {
            return false;
        }

        if (_api.IsIconic(windowHandle))
        {
            _api.RestoreWindow(windowHandle);
        }

        _api.GetWindowThreadProcessId(windowHandle, out var processId);
        _api.AllowSetForegroundWindow(processId);
        _api.SimulateAltKeyTap();
        _api.SetForegroundWindow(windowHandle);

        for (var attempt = 0; attempt < MaxPollAttempts; attempt++)
        {
            if (_api.GetForegroundWindow() == windowHandle)
            {
                return true;
            }

            Thread.Sleep(PollInterval);
        }

        var error = _api.GetLastWin32Error();
        _logger.LogWarning(
            $"Failed to bring window {windowHandle} to the foreground after {MaxPollAttempts} attempts " +
            $"(last Win32 error: {error}).");
        return false;
    }
}
