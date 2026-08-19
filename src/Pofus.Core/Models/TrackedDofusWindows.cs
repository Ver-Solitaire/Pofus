namespace Pofus.Core.Models;

/// <summary>
/// All Dofus windows the HUD must stay above simultaneously (FR-001, FR-005).
/// The HUD is not anchored to a single "active" window — it dominates the
/// z-order above every window in <see cref="Windows"/> at once.
/// </summary>
public sealed class TrackedDofusWindows
{
    public IReadOnlyList<DofusWindowInfo> Windows { get; init; } = [];

    public nint? ActiveWindowHandle { get; init; }
}
