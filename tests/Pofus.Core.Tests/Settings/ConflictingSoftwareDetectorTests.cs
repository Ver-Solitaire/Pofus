using Pofus.Core.Settings;

namespace Pofus.Core.Tests.Settings;

public class ConflictingSoftwareDetectorTests
{
    [Fact]
    public void DetectRunning_ReturnsEmptyList_WhenNoKnownProcessIsRunning()
    {
        var detector = new ConflictingSoftwareDetector(new FakeProcessController());

        Assert.Empty(detector.DetectRunning());
    }

    [Fact]
    public void DetectRunning_ReturnsMatchingProcessNames_WhenKnownProcessIsRunning()
    {
        var controller = new FakeProcessController();
        controller.Running.Add(KnownConflictingSoftware.ProcessNames[0]);
        var detector = new ConflictingSoftwareDetector(controller);

        var result = detector.DetectRunning();

        Assert.Contains(KnownConflictingSoftware.ProcessNames[0], result);
    }

    [Fact]
    public void DetectRunning_DoesNotThrow_WhenProcessControllerReportsNothing()
    {
        var detector = new ConflictingSoftwareDetector(new FakeProcessController());

        var exception = Record.Exception(() => detector.DetectRunning());

        Assert.Null(exception);
    }

    private sealed class FakeProcessController : IProcessController
    {
        public HashSet<string> Running { get; } = [];

        public bool IsRunning(string processName) => Running.Contains(processName);

        public bool TryKill(string processName, out string? error)
        {
            Running.Remove(processName);
            error = null;
            return true;
        }
    }
}
