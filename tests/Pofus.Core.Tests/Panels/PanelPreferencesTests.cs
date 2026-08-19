using Pofus.Core.Navigation;
using Pofus.Core.Panels;

namespace Pofus.Core.Tests.Panels;

public class PanelPreferencesTests
{
    private static KeyCombo CtrlAltA => new(KeyModifiers.Control | KeyModifiers.Alt, "A", 'A');

    private static KeyCombo CtrlAltB => new(KeyModifiers.Control | KeyModifiers.Alt, "B", 'B');

    [Fact]
    public void For_CreatesSettingsOnFirstAccess_DefaultingToVisibleWithNoShortcut()
    {
        var preferences = new PanelPreferences();

        var settings = preferences.For("switcher");

        Assert.False(settings.IsHidden);
        Assert.Null(settings.ShowShortcut);
    }

    [Fact]
    public void For_ReturnsTheSameInstanceOnRepeatedAccess()
    {
        var preferences = new PanelPreferences();

        preferences.For("hud").IsHidden = true;

        Assert.True(preferences.For("hud").IsHidden);
    }

    [Fact]
    public void FindShortcutConflict_ReturnsNull_WhenNothingIsBound()
    {
        var preferences = new PanelPreferences();

        Assert.Null(preferences.FindShortcutConflict(CtrlAltA, excludingPanelId: "hud"));
    }

    [Fact]
    public void FindShortcutConflict_ReturnsOtherPanel_WhenComboIsTaken()
    {
        var preferences = new PanelPreferences();
        preferences.For("switcher").ShowShortcut = CtrlAltA;

        Assert.Equal("switcher", preferences.FindShortcutConflict(CtrlAltA, excludingPanelId: "hud"));
    }

    [Fact]
    public void FindShortcutConflict_IgnoresThePanelBeingReassigned()
    {
        var preferences = new PanelPreferences();
        preferences.For("hud").ShowShortcut = CtrlAltA;

        Assert.Null(preferences.FindShortcutConflict(CtrlAltA, excludingPanelId: "hud"));
    }

    [Fact]
    public void FindShortcutConflict_ReturnsNull_WhenComboIsFree()
    {
        var preferences = new PanelPreferences();
        preferences.For("switcher").ShowShortcut = CtrlAltA;

        Assert.Null(preferences.FindShortcutConflict(CtrlAltB, excludingPanelId: "hud"));
    }

    [Fact]
    public void FindShortcutConflict_DoesNotTreatUnboundPanelsAsConflicting()
    {
        var preferences = new PanelPreferences();
        preferences.For("switcher").IsHidden = true; // exists, but no shortcut

        Assert.Null(preferences.FindShortcutConflict(CtrlAltA, excludingPanelId: "hud"));
    }
}
