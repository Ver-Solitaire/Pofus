using Pofus.Core.Models;

namespace Pofus.Core.Tests;

public class WindowPositionGuardTests
{
    // A single 1920x1080 monitor, the common case.
    private const double SingleScreenWidth = 1920;
    private const double SingleScreenHeight = 1080;

    private static (double Left, double Top) ClampOnSingleScreen(double left, double top) =>
        WindowPositionGuard.Clamp(left, top, 0, 0, SingleScreenWidth, SingleScreenHeight);

    [Fact]
    public void Clamp_LeavesPositionUntouched_WhenAlreadyOnScreen()
    {
        var clamped = ClampOnSingleScreen(640, 320);

        Assert.Equal((640, 320), clamped);
    }

    [Fact]
    public void Clamp_PullsWindowBack_WhenSavedBeyondTheRightEdge()
    {
        // e.g. saved on a second monitor to the right that has been unplugged.
        var clamped = ClampOnSingleScreen(3200, 500);

        Assert.Equal(SingleScreenWidth - WindowPositionGuard.DefaultMinimumVisibleExtent, clamped.Left);
        Assert.Equal(500, clamped.Top);
    }

    [Fact]
    public void Clamp_PullsWindowBack_WhenSavedBelowTheBottomEdge()
    {
        var clamped = ClampOnSingleScreen(200, 4000);

        Assert.Equal(200, clamped.Left);
        Assert.Equal(SingleScreenHeight - WindowPositionGuard.DefaultMinimumVisibleExtent, clamped.Top);
    }

    [Fact]
    public void Clamp_AllowsNegativeCoordinates_WhenAMonitorSitsLeftOfThePrimaryOne()
    {
        // Virtual desktop spanning a monitor placed to the left: negative
        // coordinates are legitimate there and must survive untouched.
        var clamped = WindowPositionGuard.Clamp(-1500, 40, -1920, 0, 3840, 1080);

        Assert.Equal((-1500, 40), clamped);
    }

    [Fact]
    public void Clamp_StopsAtTheDesktopOrigin_WhenSavedAboveOrLeftOfIt()
    {
        var clamped = ClampOnSingleScreen(-800, -600);

        Assert.Equal((0d, 0d), clamped);
    }

    [Theory]
    [InlineData(double.NaN, 100)]
    [InlineData(100, double.NaN)]
    [InlineData(double.PositiveInfinity, 100)]
    [InlineData(100, double.NegativeInfinity)]
    public void Clamp_FallsBackToTheDesktopOrigin_WhenACoordinateIsNotFinite(double left, double top)
    {
        // A corrupt or hand-edited preferences file must not push the window
        // somewhere unreachable.
        var clamped = WindowPositionGuard.Clamp(left, top, -1920, 0, 3840, 1080);

        Assert.Equal((-1920d, 0d), clamped);
    }

    [Fact]
    public void Clamp_NeverInvertsBounds_OnADesktopSmallerThanTheVisibleMinimum()
    {
        // Degenerate, but the arithmetic must not produce a max below the min.
        var clamped = WindowPositionGuard.Clamp(500, 500, 0, 0, 20, 20);

        Assert.Equal((0d, 0d), clamped);
    }
}
