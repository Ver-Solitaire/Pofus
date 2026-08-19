namespace Pofus.Core.Settings;

/// <summary>
/// Process names known to conflict with Pofus (ported from the reference
/// project's organizer.exe check). Deliberately a list, not a single value,
/// so more can be added later without touching the detection logic.
/// </summary>
public static class KnownConflictingSoftware
{
    public static readonly IReadOnlyList<string> ProcessNames = ["organizer"];
}
