namespace Pofus.Core.Accounts;

/// <summary>
/// Persisted, pseudo-keyed account preferences
/// (%APPDATA%\Pofus\account-preferences.json). The only durable state for
/// this module — <see cref="AccountSession"/> is rebuilt from this on every
/// refresh.
/// </summary>
public sealed class AccountPreferences
{
    public const string DefaultTeamName = "Équipe 1";
    public const int MaxCustomOrderEntries = 50;

    public Dictionary<string, bool> ActiveByPseudo { get; set; } = [];

    public Dictionary<string, string> TeamByPseudo { get; set; } = [];

    public string? LeaderPseudo { get; set; }

    public List<string> CustomOrder { get; set; } = [];
}
