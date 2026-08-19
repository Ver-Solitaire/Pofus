namespace Pofus.Core.Accounts;

/// <summary>
/// Deterministically derives a distinct color per class name, so the switcher
/// widget can tell characters apart at a glance without needing real class
/// icon assets (still deferred — see feature 001 tasks.md T027). Same class
/// name always yields the same color, including "Classe inconnue".
/// </summary>
public static class ClassColorPalette
{
    public readonly record struct Rgb(byte R, byte G, byte B);

    public static Rgb ForClassName(string className)
    {
        var hue = StableHash(className) % 360;
        return HslToRgb(hue, 0.55, 0.50);
    }

    private static uint StableHash(string text)
    {
        // FNV-1a — stable across processes/runs, unlike string.GetHashCode().
        unchecked
        {
            var hash = 2166136261;
            foreach (var c in text)
            {
                hash = (hash ^ c) * 16777619;
            }

            return hash;
        }
    }

    private static Rgb HslToRgb(double hue, double saturation, double lightness)
    {
        var c = (1 - Math.Abs(2 * lightness - 1)) * saturation;
        var x = c * (1 - Math.Abs(hue / 60.0 % 2 - 1));
        var m = lightness - c / 2;

        var (r1, g1, b1) = hue switch
        {
            < 60 => (c, x, 0.0),
            < 120 => (x, c, 0.0),
            < 180 => (0.0, c, x),
            < 240 => (0.0, x, c),
            < 300 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };

        return new Rgb((byte)((r1 + m) * 255), (byte)((g1 + m) * 255), (byte)((b1 + m) * 255));
    }
}
