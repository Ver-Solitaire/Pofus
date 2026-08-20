using Pofus.Core.Appearance;

namespace Pofus.Core.Tests.Appearance;

public class ThemeColorTests
{
    [Theory]
    [InlineData("#1E1E26", 0x1E, 0x1E, 0x26)]
    [InlineData("1E1E26", 0x1E, 0x1E, 0x26)]
    [InlineData("  #ffffff  ", 0xFF, 0xFF, 0xFF)]
    public void TryParseHex_AcceptsValidForms(string text, byte r, byte g, byte b)
    {
        var parsed = ThemeColor.TryParseHex(text);

        Assert.Equal(new ThemeColor(r, g, b), parsed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    [InlineData("#GGGGGG")]
    public void TryParseHex_RejectsInvalidForms(string? text) =>
        Assert.Null(ThemeColor.TryParseHex(text));

    [Fact]
    public void ToHex_RoundTripsThroughTryParseHex()
    {
        var color = new ThemeColor(0x0A, 0xB2, 0xFF);

        Assert.Equal(color, ThemeColor.TryParseHex(color.ToHex()));
    }

    [Fact]
    public void Lighten_MovesTowardWhite_AndDarken_MovesTowardBlack()
    {
        var color = new ThemeColor(100, 100, 100);

        Assert.True(color.Lighten(0.5).R > color.R);
        Assert.True(color.Darken(0.5).R < color.R);
    }

    [Fact]
    public void Lighten_AndDarken_SaturateRatherThanOverflow()
    {
        Assert.Equal(new ThemeColor(255, 255, 255), new ThemeColor(255, 255, 255).Lighten(1));
        Assert.Equal(new ThemeColor(0, 0, 0), new ThemeColor(0, 0, 0).Darken(1));
    }
}

public class AppearancePreferencesTests
{
    /// <summary>The off-white Pofus shipped with before the switch to pure white.</summary>
    private static readonly ThemeColor LegacyText = new(0xF2, 0xEE, 0xE6);

    [Fact]
    public void CreateDefault_UsesPureWhiteText()
    {
        Assert.Equal(new ThemeColor(0xFF, 0xFF, 0xFF), AppearancePreferences.CreateDefault().Text);
    }

    [Fact]
    public void UpgradeLegacyTextColor_MovesAnUntouchedFileToPureWhite()
    {
        var preferences = new AppearancePreferences { Text = LegacyText };

        Assert.True(preferences.UpgradeLegacyTextColor());
        Assert.Equal(new ThemeColor(0xFF, 0xFF, 0xFF), preferences.Text);
    }

    [Fact]
    public void UpgradeLegacyTextColor_LeavesAColourTheUserActuallyChose()
    {
        var chosen = new ThemeColor(0x8F, 0xD8, 0xFF);
        var preferences = new AppearancePreferences { Text = chosen };

        Assert.False(preferences.UpgradeLegacyTextColor());
        Assert.Equal(chosen, preferences.Text);
    }

    [Fact]
    public void UpgradeLegacyTextColor_IsIdempotent()
    {
        var preferences = new AppearancePreferences { Text = LegacyText };
        preferences.UpgradeLegacyTextColor();

        Assert.False(preferences.UpgradeLegacyTextColor());
    }

    [Fact]
    public void ClampOpacities_EnforcesTheFloor_SoAWindowCanNeverBecomeInvisible()
    {
        var preferences = new AppearancePreferences { BackgroundOpacity = 0.0, BorderOpacity = -5 };

        preferences.ClampOpacities();

        Assert.Equal(AppearancePreferences.MinOpacity, preferences.BackgroundOpacity);
        Assert.Equal(AppearancePreferences.MinOpacity, preferences.BorderOpacity);
    }

    [Fact]
    public void ClampOpacities_CapsAboveOne()
    {
        var preferences = new AppearancePreferences { BackgroundOpacity = 3, BorderOpacity = 1.5 };

        preferences.ClampOpacities();

        Assert.Equal(1.0, preferences.BackgroundOpacity);
        Assert.Equal(1.0, preferences.BorderOpacity);
    }

    [Fact]
    public void Clone_DoesNotShareStateWithTheOriginal()
    {
        var original = AppearancePreferences.CreateDefault();
        var copy = original.Clone();

        copy.BackgroundOpacity = 0.5;
        copy.Text = new ThemeColor(1, 2, 3);

        Assert.Equal(1.0, original.BackgroundOpacity);
        Assert.NotEqual(copy.Text, original.Text);
    }

    [Fact]
    public void Presets_AreIndependentInstances_SoApplyingOneCannotMutateTheCatalogue()
    {
        var first = AppearancePresets.All[1].Values;
        first.BackgroundOpacity = 0.99;

        Assert.NotEqual(0.99, AppearancePresets.All[1].Values.BackgroundOpacity);
    }
}

public class ThemePaletteTests
{
    [Fact]
    public void Build_CoversEveryThemeKeyExactlyOnce()
    {
        var values = ThemePalette.Build(AppearancePreferences.CreateDefault());

        var keys = values.Select(v => v.Key).ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());
        Assert.Contains(ThemePalette.Surface, keys);
        Assert.Contains(ThemePalette.TextPrimary, keys);
        Assert.Contains(ThemePalette.BorderSubtle, keys);
    }

    [Fact]
    public void Build_NeverTouchesTheFunctionalAccentColors()
    {
        var keys = ThemePalette.Build(AppearancePreferences.CreateDefault()).Select(v => v.Key).ToList();

        // Accent and danger mark the leader, the focused account and errors:
        // they must stay legible whatever the user picks (spec.md Assumptions).
        Assert.DoesNotContain("Pofus.Accent", keys);
        Assert.DoesNotContain("Pofus.Danger", keys);
    }

    [Fact]
    public void Build_AppliesBackgroundOpacityToSurfacesOnly()
    {
        var preferences = new AppearancePreferences { BackgroundOpacity = 0.5, BorderOpacity = 1.0 };

        var values = ThemePalette.Build(preferences).ToDictionary(v => v.Key, v => v.A);

        Assert.Equal(128, values[ThemePalette.Surface]);
        Assert.Equal(128, values[ThemePalette.Bg]);
        Assert.Equal(255, values[ThemePalette.BorderSubtle]);
        Assert.Equal(255, values[ThemePalette.TextPrimary]);
    }

    [Fact]
    public void Build_FadesMutedAndDisabledTextViaAlpha_KeepingTheChosenHue()
    {
        var text = new ThemeColor(0x11, 0x22, 0x33);
        var values = ThemePalette.Build(new AppearancePreferences { Text = text })
            .ToDictionary(v => v.Key, v => v);

        Assert.Equal(text, values[ThemePalette.TextPrimary].Color);
        Assert.Equal(text, values[ThemePalette.TextMuted].Color);
        Assert.True(values[ThemePalette.TextMuted].A < values[ThemePalette.TextPrimary].A);
        Assert.True(values[ThemePalette.TextDisabled].A < values[ThemePalette.TextMuted].A);
    }
}
