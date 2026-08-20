namespace Pofus.Core.Appearance;

/// <summary>
/// The user's global look for every Pofus window: interior, text and borders
/// (%APPDATA%\Pofus\appearance.json). Accent and danger colors are deliberately
/// absent — they mark the leader, the focused account and errors, and must stay
/// legible whatever the theme (spec.md Assumptions).
/// </summary>
public sealed class AppearancePreferences
{
    /// <summary>
    /// Below this, a window would be invisible and effectively lost, since the
    /// preview is live (FR-009). Enforced on load and on every change, so even a
    /// hand-edited file cannot get past it.
    /// </summary>
    public const double MinOpacity = 0.15;

    /// <summary>
    /// The warm off-white text colour Pofus shipped with before 1.1.1. Kept so
    /// <see cref="UpgradeLegacyTextColor"/> can recognise a file that simply
    /// carries the old default and has never been customised.
    /// </summary>
    private static readonly ThemeColor LegacyDefaultText = new(0xF2, 0xEE, 0xE6);

    public ThemeColor Background { get; set; } = new(0x1E, 0x1E, 0x26);

    public double BackgroundOpacity { get; set; } = 1.0;

    public ThemeColor Text { get; set; } = new(0xFF, 0xFF, 0xFF);

    public ThemeColor Border { get; set; } = new(0x34, 0x34, 0x3F);

    public double BorderOpacity { get; set; } = 1.0;

    /// <summary>The current theme, so an untouched install looks unchanged.</summary>
    public static AppearancePreferences CreateDefault() => new();

    /// <summary>
    /// Moves a saved file still holding the old off-white text colour onto the
    /// new pure white, and reports whether it changed anything.
    ///
    /// Only the exact former default is touched: a colour the user actually
    /// picked — including a deliberate off-white — is left alone, since the
    /// point is to update an untouched install, not to overrule a choice.
    /// </summary>
    public bool UpgradeLegacyTextColor()
    {
        if (Text != LegacyDefaultText)
        {
            return false;
        }

        Text = new ThemeColor(0xFF, 0xFF, 0xFF);
        return true;
    }

    public void ClampOpacities()
    {
        BackgroundOpacity = Math.Clamp(BackgroundOpacity, MinOpacity, 1.0);
        BorderOpacity = Math.Clamp(BorderOpacity, MinOpacity, 1.0);
    }

    public AppearancePreferences Clone() => new()
    {
        Background = Background,
        BackgroundOpacity = BackgroundOpacity,
        Text = Text,
        Border = Border,
        BorderOpacity = BorderOpacity,
    };
}
