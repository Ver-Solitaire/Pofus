using System.Windows;
using System.Windows.Media;
using Pofus.Core.Appearance;
using Pofus.Core.Logging;

namespace Pofus.Hud.Appearance;

/// <summary>
/// Applies the user's appearance settings by replacing the themed brushes in
/// the application's resources.
///
/// The themed keys are consumed through <c>{DynamicResource Pofus.X}</c>, so
/// replacing an entry repaints every consumer at once — already-open windows
/// included, and colors used inside the theme's own Styles and
/// ControlTemplates.
///
/// Mutating the brushes in place would be simpler, but WPF freezes Freezables
/// loaded from a ResourceDictionary: their <c>Color</c> is read-only at
/// runtime. Hence replacement plus DynamicResource, which is also why the
/// themed keys must not be referenced with StaticResource anywhere.
/// </summary>
public sealed class ThemeApplier
{
    private readonly ResourceDictionary _resources;
    private readonly IAppLogger _logger;

    public ThemeApplier(ResourceDictionary resources, IAppLogger logger)
    {
        _resources = resources;
        _logger = logger;
    }

    public void Apply(AppearancePreferences preferences)
    {
        preferences.ClampOpacities();

        foreach (var (key, alpha, color) in ThemePalette.Build(preferences))
        {
            if (!_resources.Contains(key))
            {
                // A renamed or removed theme key must not break the rest of the
                // repaint — report it, skip it (Principe I).
                _logger.LogWarning($"Theme key '{key}' is not defined; appearance not applied to it.");
                continue;
            }

            var brush = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
            brush.Freeze(); // never mutated afterwards; frozen brushes render faster
            _resources[key] = brush;
        }
    }
}
