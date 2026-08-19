namespace Pofus.Core.Settings;

public interface IConflictingSoftwareDetector
{
    /// <summary>Returns the names of known conflicting processes currently running. Never throws.</summary>
    IReadOnlyList<string> DetectRunning();
}

/// <summary>
/// Checks <see cref="KnownConflictingSoftware"/> against currently running
/// processes at startup (FR-001) — pure logic, no direct Windows dependency.
/// </summary>
public sealed class ConflictingSoftwareDetector : IConflictingSoftwareDetector
{
    private readonly IProcessController _processController;

    public ConflictingSoftwareDetector(IProcessController processController)
    {
        _processController = processController;
    }

    public IReadOnlyList<string> DetectRunning() =>
        KnownConflictingSoftware.ProcessNames.Where(_processController.IsRunning).ToList();
}
