using System.Windows;
using Pofus.Core.Navigation;
using Pofus.Core.Panels;
using Pofus.Hud.Modules.Navigation;

namespace Pofus.Hud.Panels;

/// <summary>A Pofus window the user can hide and bring back. Declaring one is
/// all it takes to make a new window hideable (SC-005).</summary>
public sealed record HideablePanel(string PanelId, string DisplayName, Window Window);

/// <summary>
/// The generic show/hide engine for Pofus's own windows (feature 005). It
/// knows nothing about any specific window: each one registers itself, and
/// this handles persistence, the global un-hide shortcuts, and the state
/// restored at startup.
///
/// Hiding uses Window.Hide() rather than Close(), so a window keeps its
/// position and internal state and comes back exactly where it was (FR-010).
/// </summary>
public sealed class PanelVisibilityService
{
    private const string ActionIdPrefix = "panel:";

    private readonly IPanelPreferencesStore _store;
    private readonly List<HideablePanel> _panels = [];
    private PanelPreferences _preferences = new();
    private GlobalHotkeyListener? _hotkeyListener;

    public PanelVisibilityService(IPanelPreferencesStore store)
    {
        _store = store;
    }

    public static string ToActionId(string panelId) => ActionIdPrefix + panelId;

    public static bool TryParseActionId(string actionId, out string panelId)
    {
        if (actionId.StartsWith(ActionIdPrefix, StringComparison.Ordinal))
        {
            panelId = actionId[ActionIdPrefix.Length..];
            return true;
        }

        panelId = string.Empty;
        return false;
    }

    public void Register(HideablePanel panel) => _panels.Add(panel);

    public IReadOnlyList<HideablePanel> GetPanels() => _panels;

    public bool IsHidden(string panelId) => _preferences.For(panelId).IsHidden;

    public KeyCombo? GetShortcut(string panelId) => _preferences.For(panelId).ShowShortcut;

    /// <summary>
    /// Loads persisted state. MUST be awaited before the host window is shown,
    /// because <see cref="AttachShortcuts"/> runs from that window's Loaded
    /// event and can only register shortcuts it already knows about.
    /// </summary>
    public async Task LoadAsync() => _preferences = await _store.LoadAsync();

    /// <summary>
    /// Re-hides whatever the user had hidden (FR-007). Call after the windows
    /// have been shown — Hide() before Show() would simply be undone.
    /// </summary>
    public void ApplyPersistedState()
    {
        foreach (var panel in _panels)
        {
            if (_preferences.For(panel.PanelId).IsHidden)
            {
                panel.Window.Hide();
            }
        }
    }

    public void Hide(string panelId) => SetHidden(panelId, hidden: true);

    /// <summary>Brings a window back. Idempotent — showing a visible window does nothing.</summary>
    public void Show(string panelId) => SetHidden(panelId, hidden: false);

    private void SetHidden(string panelId, bool hidden)
    {
        var panel = _panels.FirstOrDefault(
            p => string.Equals(p.PanelId, panelId, StringComparison.OrdinalIgnoreCase));
        if (panel is null)
        {
            return;
        }

        var settings = _preferences.For(panel.PanelId);
        if (settings.IsHidden == hidden)
        {
            return;
        }

        settings.IsHidden = hidden;
        if (hidden)
        {
            panel.Window.Hide();
        }
        else
        {
            panel.Window.Show();
        }

        PersistInBackground();
    }

    /// <summary>
    /// Registers the persisted un-hide shortcuts and routes their presses.
    /// Call once the host window is Loaded (RegisterHotKey needs a real HWND).
    /// </summary>
    public void AttachShortcuts(GlobalHotkeyListener listener)
    {
        _hotkeyListener = listener;
        listener.HotkeyPressed += OnHotkeyPressed;

        foreach (var panel in _panels)
        {
            var combo = _preferences.For(panel.PanelId).ShowShortcut;
            if (combo is not null)
            {
                // A failure here (combo already claimed system-wide) is logged
                // by the listener and must not stop the others (FR-011).
                listener.RegisterOrReplace(ToActionId(panel.PanelId), combo);
            }
        }
    }

    /// <summary>Binds a new shortcut to a panel, effective immediately (FR-008).</summary>
    public async Task SetShortcutAsync(string panelId, KeyCombo combo)
    {
        _preferences.For(panelId).ShowShortcut = combo;
        _hotkeyListener?.RegisterOrReplace(ToActionId(panelId), combo);
        await _store.SaveAsync(_preferences);
    }

    /// <summary>Conflicting panel id for this combo, if another panel already uses it (FR-006).</summary>
    public string? FindShortcutConflict(KeyCombo combo, string excludingPanelId) =>
        _preferences.FindShortcutConflict(combo, excludingPanelId);

    public string DescribePanel(string panelId) =>
        _panels.FirstOrDefault(p => string.Equals(p.PanelId, panelId, StringComparison.OrdinalIgnoreCase))
            ?.DisplayName
        ?? panelId;

    private void OnHotkeyPressed(string actionId)
    {
        // The listener is shared with the navigation actions ("nav:*").
        if (TryParseActionId(actionId, out var panelId))
        {
            Show(panelId);
        }
    }

    private async void PersistInBackground() => await _store.SaveAsync(_preferences);
}
