namespace Pofus.Core.Appearance;

/// <summary>A theme brush key and the ARGB color it should take.</summary>
public readonly record struct ThemeBrushValue(string Key, byte A, ThemeColor Color);

/// <summary>
/// Turns the three user-facing settings into the ~10 concrete theme brush
/// colors. Pure arithmetic on color channels, so it is fully unit-testable
/// without WPF (Principe III) — the WPF side only assigns the results.
/// </summary>
public static class ThemePalette
{
    // Keys deliberately excluded: Pofus.Accent / Pofus.Danger carry meaning
    // (leader, focus, error) and must stay readable under any theme.
    public const string Bg = "Pofus.Bg";
    public const string Surface = "Pofus.Surface";
    public const string SurfaceRaised = "Pofus.SurfaceRaised";
    public const string TextPrimary = "Pofus.TextPrimary";
    public const string TextMuted = "Pofus.TextMuted";
    public const string TextDisabled = "Pofus.TextDisabled";
    public const string BorderSubtle = "Pofus.BorderSubtle";
    public const string BorderStrong = "Pofus.BorderStrong";

    public static IReadOnlyList<ThemeBrushValue> Build(AppearancePreferences preferences)
    {
        var backgroundAlpha = ToAlpha(preferences.BackgroundOpacity);
        var borderAlpha = ToAlpha(preferences.BorderOpacity);
        var background = preferences.Background;
        var text = preferences.Text;
        var border = preferences.Border;

        return
        [
            // Surfaces: one shade darker than the base for the deepest layer,
            // progressively lighter for raised and hovered ones.
            new(Bg, backgroundAlpha, background.Darken(0.25)),
            new(Surface, backgroundAlpha, background),
            new(SurfaceRaised, backgroundAlpha, background.Lighten(0.10)),

            // Text keeps its hue; the muted/disabled variants fade via alpha so
            // they stay correct over any background color. Muted stays clearly
            // the same colour as primary, just quieter — it marks secondary
            // information, it is not meant to read as grey.
            new(TextPrimary, 0xFF, text),
            new(TextMuted, 0xCC, text),
            new(TextDisabled, 0x66, text),

            new(BorderSubtle, borderAlpha, border),
            new(BorderStrong, borderAlpha, border.Lighten(0.18)),
        ];
    }

    private static byte ToAlpha(double opacity) =>
        (byte)Math.Round(Math.Clamp(opacity, 0, 1) * 255);
}
