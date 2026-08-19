using Pofus.Platform;

namespace Pofus.Platform.Tests;

public class TopmostWindowControllerTests
{
    [Fact]
    public void BringToTop_CallsSetWindowPosWithHwndTopmost()
    {
        var api = new FakeWin32WindowApi();
        var controller = new TopmostWindowController(new FakeAppLogger(), api);
        var handle = new nint(123);

        controller.BringToTop(handle);

        var call = Assert.Single(api.SetWindowPosCalls);
        Assert.Equal(handle, call.Handle);
        Assert.Equal(new nint(-1), call.InsertAfter); // HWND_TOPMOST
    }

    [Fact]
    public void BringToTop_DoesNotThrow_AndLogsWarning_WhenSetWindowPosFails()
    {
        var api = new FakeWin32WindowApi { SetWindowPosSucceeds = false };
        var logger = new FakeAppLogger();
        var controller = new TopmostWindowController(logger, api);

        var exception = Record.Exception(() => controller.BringToTop(new nint(1)));

        Assert.Null(exception);
        Assert.NotEmpty(logger.Warnings);
    }

    [Fact]
    public void BringToTop_CanBeCalledRepeatedly_ForEveryForegroundWindowChange()
    {
        var api = new FakeWin32WindowApi();
        var controller = new TopmostWindowController(new FakeAppLogger(), api);
        var hudHandle = new nint(1);

        // Simulates reasserting topmost on each of several foreground window
        // changes across multiple Dofus accounts (FR-005, FR-009).
        for (var i = 0; i < 8; i++)
        {
            controller.BringToTop(hudHandle);
        }

        Assert.Equal(8, api.SetWindowPosCalls.Count);
    }
}
