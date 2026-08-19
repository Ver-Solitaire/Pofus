using System.Globalization;

namespace Pofus.Core.Appearance;

/// <summary>
/// An RGB color kept independent of WPF so the appearance model and its shade
/// derivations stay unit-testable without Windows (Principe III).
/// </summary>
public readonly record struct ThemeColor(byte R, byte G, byte B)
{
    public static ThemeColor? TryParseHex(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var hex = text.Trim().TrimStart('#');
        if (hex.Length != 6)
        {
            return null;
        }

        return byte.TryParse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r)
            && byte.TryParse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g)
            && byte.TryParse(hex[4..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b)
                ? new ThemeColor(r, g, b)
                : null;
    }

    public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";

    /// <summary>Moves each channel toward white; <paramref name="amount"/> is 0..1.</summary>
    public ThemeColor Lighten(double amount) => new(
        Blend(R, 255, amount), Blend(G, 255, amount), Blend(B, 255, amount));

    /// <summary>Moves each channel toward black; <paramref name="amount"/> is 0..1.</summary>
    public ThemeColor Darken(double amount) => new(
        Blend(R, 0, amount), Blend(G, 0, amount), Blend(B, 0, amount));

    private static byte Blend(byte from, byte to, double amount)
    {
        var clamped = Math.Clamp(amount, 0, 1);
        return (byte)Math.Round(from + ((to - from) * clamped));
    }
}
