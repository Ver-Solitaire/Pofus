using Pofus.Core.Navigation;

namespace Pofus.Core.Tests.Navigation;

public class NavigationShortcutPreferencesTests
{
    [Fact]
    public void CreateDefaults_AssignsThreeDistinctModifierBasedCombos()
    {
        var defaults = NavigationShortcutPreferences.CreateDefaults();

        Assert.Equal(3, defaults.Count);
        Assert.All(defaults.Values, combo => Assert.NotEqual(KeyModifiers.None, combo.Modifiers));
    }

    [Fact]
    public void FindConflict_ReturnsNull_WhenComboIsUnique()
    {
        var preferences = new NavigationShortcutPreferences();

        var conflict = preferences.FindConflict(
            new KeyCombo(KeyModifiers.Alt, "F9", 0x78), excluding: NavigationAction.GoToLeader);

        Assert.Null(conflict);
    }

    [Fact]
    public void FindConflict_ReturnsTheOtherAction_WhenComboIsAlreadyBound()
    {
        var preferences = new NavigationShortcutPreferences();
        var nextCombo = preferences.Bindings[NavigationAction.Next];

        var conflict = preferences.FindConflict(nextCombo, excluding: NavigationAction.Previous);

        Assert.Equal(NavigationAction.Next, conflict);
    }

    [Fact]
    public void FindConflict_IgnoresTheActionBeingReassigned()
    {
        var preferences = new NavigationShortcutPreferences();
        var nextCombo = preferences.Bindings[NavigationAction.Next];

        // Re-assigning Next to its own current combo isn't a conflict with itself.
        var conflict = preferences.FindConflict(nextCombo, excluding: NavigationAction.Next);

        Assert.Null(conflict);
    }
}
