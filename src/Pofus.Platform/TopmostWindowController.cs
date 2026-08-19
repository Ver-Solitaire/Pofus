using Pofus.Core.Logging;

namespace Pofus.Platform;

public interface ITopmostWindowController
{
    /// <summary>
    /// Reasserts HWND_TOPMOST for the given window without moving, resizing, or
    /// stealing input focus. Safe to call repeatedly (e.g. on every foreground
    /// window change) — never throws.
    /// </summary>
    void BringToTop(nint windowHandle);
}

public sealed class TopmostWindowController : ITopmostWindowController
{
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoActivate = 0x0010;
    private static readonly nint HwndTopmost = new(-1);

    private readonly IWin32WindowApi _api;
    private readonly IAppLogger _logger;

    public TopmostWindowController(IAppLogger logger)
        : this(logger, new Win32WindowApi())
    {
    }

    public TopmostWindowController(IAppLogger logger, IWin32WindowApi api)
    {
        _logger = logger;
        _api = api;
    }

    public void BringToTop(nint windowHandle)
    {
        var success = _api.SetWindowPos(
            windowHandle, HwndTopmost, 0, 0, 0, 0,
            SwpNoMove | SwpNoSize | SwpNoActivate);

        if (!success)
        {
            var error = _api.GetLastWin32Error();
            _logger.LogWarning(
                $"SetWindowPos(HWND_TOPMOST) failed with Win32 error {error} for handle {windowHandle}.");
        }
    }
}
