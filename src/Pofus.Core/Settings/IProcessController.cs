namespace Pofus.Core.Settings;

/// <summary>
/// Abstraction over process detection/termination. Lives in Pofus.Core (not
/// Pofus.Platform) so <see cref="ConflictingSoftwareDetector"/> stays
/// testable without Windows, same pattern as IDofusWindowLocator.
/// </summary>
public interface IProcessController
{
    bool IsRunning(string processName);

    /// <summary>Returns true on success; on failure returns false and sets <paramref name="error"/>.</summary>
    bool TryKill(string processName, out string? error);
}
