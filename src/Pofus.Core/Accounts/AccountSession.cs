namespace Pofus.Core.Accounts;

/// <summary>
/// In-memory view of a detected account, rebuilt on every refresh by merging
/// raw window detection with persisted <see cref="AccountPreferences"/>.
/// Never persisted as-is — <see cref="Pseudo"/> is the only stable key across
/// reconnections; <see cref="WindowHandle"/> changes every time.
/// </summary>
public sealed class AccountSession
{
    public required string Pseudo { get; init; }

    public required string ClassName { get; init; }

    public required nint WindowHandle { get; init; }

    public bool IsActive { get; init; } = true;

    public required string Team { get; init; }

    public bool IsLeader { get; init; }
}
