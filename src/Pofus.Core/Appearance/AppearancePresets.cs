namespace Pofus.Core.Appearance;

public sealed record AppearancePreset(string Name, AppearancePreferences Values);

/// <summary>
/// Ready-made looks the user can apply in one action and then fine-tune
/// (FR-006). Each returns a fresh instance so applying a preset never leaves
/// the caller sharing state with the catalogue.
/// </summary>
public static class AppearancePresets
{
    public static IReadOnlyList<AppearancePreset> All =>
    [
        new("Sombre", AppearancePreferences.CreateDefault()),

        new("Verre translucide", new AppearancePreferences
        {
            Background = new ThemeColor(0x10, 0x14, 0x20),
            BackgroundOpacity = 0.45,
            Text = new ThemeColor(0xEC, 0xF6, 0xFF),
            Border = new ThemeColor(0x8F, 0xD8, 0xFF),
            BorderOpacity = 0.75,
        }),

        new("Contrasté", new AppearancePreferences
        {
            Background = new ThemeColor(0x0B, 0x0B, 0x0F),
            BackgroundOpacity = 1.0,
            Text = new ThemeColor(0xFF, 0xFF, 0xFF),
            Border = new ThemeColor(0xB9, 0xB9, 0xC6),
            BorderOpacity = 1.0,
        }),
    ];
}
