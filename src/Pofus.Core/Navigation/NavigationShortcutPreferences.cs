namespace Pofus.Core.Navigation;

/// <summary>
/// Persisted, per-action keyboard shortcut bindings
/// (%APPDATA%\Pofus\navigation-shortcuts.json). Defaults use modifier-based
/// combos (unlike the reference project's bare "tab"/"²") to avoid
/// interfering with normal typing or in-game controls (spec.md Assumptions).
/// </summary>
public sealed class NavigationShortcutPreferences
{
    public Dictionary<NavigationAction, KeyCombo> Bindings { get; set; } = CreateDefaults();

    public static Dictionary<NavigationAction, KeyCombo> CreateDefaults() => new()
    {
        [NavigationAction.Next] = new KeyCombo(KeyModifiers.Control, "Tab", 0x09),
        [NavigationAction.Previous] = new KeyCombo(KeyModifiers.Control | KeyModifiers.Shift, "Tab", 0x09),
        [NavigationAction.GoToLeader] = new KeyCombo(KeyModifiers.Control, "L", 'L'),
    };

    /// <summary>Returns the other action already bound to this combo, if any (FR-006).</summary>
    public NavigationAction? FindConflict(KeyCombo combo, NavigationAction excluding)
    {
        foreach (var (action, bound) in Bindings)
        {
            if (action != excluding && bound == combo)
            {
                return action;
            }
        }

        return null;
    }
}
