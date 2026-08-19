using System.Windows;
using System.Windows.Interop;
using Pofus.Platform;

namespace Pofus.Hud;

/// <summary>
/// Keeps a Pofus window above the game.
///
/// <c>Topmost="True"</c> alone is not enough here: activating a Dofus window
/// pushes it to the top of the z-order and Pofus ends up behind it. So the
/// topmost flag is re-asserted whenever the foreground window changes —
/// the same approach HudWindow and the switcher widget already use.
///
/// Our own windows are skipped: re-asserting while a Pofus popup (a context
/// menu, a dropdown) is opening would raise this window above it and hide it.
/// </summary>
public sealed class TopmostKeeper
{
    private readonly Window _window;
    private readonly ITopmostWindowController _topmostController;
    private readonly ForegroundWindowWatcher _foregroundWatcher;
    private readonly IWin32WindowApi _win32Api;
    private readonly uint _ownProcessId = (uint)Environment.ProcessId;
    private nint _selfHandle;

    private TopmostKeeper(
        Window window,
        ITopmostWindowController topmostController,
        ForegroundWindowWatcher foregroundWatcher,
        IWin32WindowApi win32Api)
    {
        _window = window;
        _topmostController = topmostController;
        _foregroundWatcher = foregroundWatcher;
        _win32Api = win32Api;
    }

    /// <summary>
    /// Starts keeping <paramref name="window"/> on top, and stops when it closes.
    /// The watcher is shared, so this only subscribes — it never starts or
    /// disposes it.
    /// </summary>
    public static void Attach(
        Window window,
        ITopmostWindowController topmostController,
        ForegroundWindowWatcher foregroundWatcher,
        IWin32WindowApi win32Api)
    {
        var keeper = new TopmostKeeper(window, topmostController, foregroundWatcher, win32Api);
        window.Loaded += keeper.OnLoaded;
        window.Closed += keeper.OnClosed;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _selfHandle = new WindowInteropHelper(_window).Handle;
        _topmostController.BringToTop(_selfHandle);
        _foregroundWatcher.ForegroundWindowChanged += OnForegroundWindowChanged;
    }

    private void OnClosed(object? sender, EventArgs e) =>
        _foregroundWatcher.ForegroundWindowChanged -= OnForegroundWindowChanged;

    private void OnForegroundWindowChanged(nint newForegroundWindow)
    {
        if (newForegroundWindow == _selfHandle || IsOwnProcessWindow(newForegroundWindow))
        {
            return;
        }

        // SetWinEventHook callbacks arrive off the WPF dispatcher thread.
        _window.Dispatcher.Invoke(() => _topmostController.BringToTop(_selfHandle));
    }

    private bool IsOwnProcessWindow(nint windowHandle)
    {
        if (windowHandle == nint.Zero)
        {
            return false;
        }

        _win32Api.GetWindowThreadProcessId(windowHandle, out var processId);
        return processId == _ownProcessId;
    }
}
