using Pofus.Core.Navigation;

namespace Pofus.Core.Panels;

/// <summary>Per-window persisted state: hidden or not, and its optional un-hide shortcut.</summary>
public sealed class PanelSettings
{
    public bool IsHidden { get; set; }

    /// <summary>Null when the user has not bound a shortcut to this window.</summary>
    public KeyCombo? ShowShortcut { get; set; }
}

/// <summary>
/// Visibility and shortcut of every hideable Pofus window
/// (%APPDATA%\Pofus\panels.json). Keyed on an open-ended panel id rather than
/// an enum so declaring a new window needs no change here (FR-012).
///
/// No shortcut is pre-assigned: a default could silently collide with the
/// navigation shortcuts, which DO ship with defaults (spec.md Assumptions).
/// </summary>
public sealed class PanelPreferences
{
    public Dictionary<string, PanelSettings> Panels { get; set; } = [];

    /// <summary>Settings for a panel, created on first access.</summary>
    public PanelSettings For(string panelId)
    {
        if (!Panels.TryGetValue(panelId, out var settings))
        {
            settings = new PanelSettings();
            Panels[panelId] = settings;
        }

        return settings;
    }

    /// <summary>
    /// Returns the id of another panel already bound to this combo, if any
    /// (FR-006). Cross-checking against the navigation shortcuts is the
    /// caller's job — it holds both preference objects.
    /// </summary>
    public string? FindShortcutConflict(KeyCombo combo, string excludingPanelId)
    {
        foreach (var (panelId, settings) in Panels)
        {
            if (!string.Equals(panelId, excludingPanelId, StringComparison.OrdinalIgnoreCase)
                && settings.ShowShortcut == combo)
            {
                return panelId;
            }
        }

        return null;
    }
}
