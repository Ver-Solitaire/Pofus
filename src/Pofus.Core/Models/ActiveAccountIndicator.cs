namespace Pofus.Core.Models;

/// <summary>
/// Which account a targeted HUD action currently applies to (FR-009). Distinct
/// from <see cref="TrackedDofusWindows"/>: the HUD stays above every window it
/// pilots even while only one account is "active" for a targeted action.
/// </summary>
public sealed class ActiveAccountIndicator
{
    public string? ActiveAccountLabel { get; init; }
}
