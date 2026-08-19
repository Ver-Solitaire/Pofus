namespace Pofus.Core.Navigation;

[Flags]
public enum KeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8,
}

/// <summary>
/// A keyboard shortcut, independent of Win32/WPF — <see cref="VirtualKeyCode"/>
/// carries the raw Win32 VK value (needed by <c>RegisterHotKey</c> in
/// Pofus.Platform) as plain data, not a P/Invoke call, so this stays testable
/// without Windows (Principe III).
/// </summary>
public sealed record KeyCombo(KeyModifiers Modifiers, string Key, uint VirtualKeyCode)
{
    private static readonly IReadOnlyDictionary<string, uint> SupportedKeys = BuildKeyTable();

    public static KeyCombo? TryParse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        var modifiers = KeyModifiers.None;
        string? mainKey = null;

        foreach (var part in parts)
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    modifiers |= KeyModifiers.Control;
                    break;
                case "alt":
                    modifiers |= KeyModifiers.Alt;
                    break;
                case "shift":
                case "maj":
                    modifiers |= KeyModifiers.Shift;
                    break;
                case "win":
                    modifiers |= KeyModifiers.Win;
                    break;
                default:
                    if (mainKey is not null)
                    {
                        // More than one non-modifier token — not a valid single combo.
                        return null;
                    }

                    mainKey = part;
                    break;
            }
        }

        if (mainKey is null)
        {
            return null;
        }

        var normalized = NormalizeKeyName(mainKey);
        return SupportedKeys.TryGetValue(normalized, out var vk)
            ? new KeyCombo(modifiers, normalized, vk)
            : null;
    }

    public string ToDisplayString()
    {
        var parts = new List<string>();
        if (Modifiers.HasFlag(KeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (Modifiers.HasFlag(KeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (Modifiers.HasFlag(KeyModifiers.Shift))
        {
            parts.Add("Maj");
        }

        if (Modifiers.HasFlag(KeyModifiers.Win))
        {
            parts.Add("Win");
        }

        parts.Add(Key);
        return string.Join("+", parts);
    }

    private static string NormalizeKeyName(string key) =>
        key.Length == 1 ? key.ToUpperInvariant() : Capitalize(key);

    private static string Capitalize(string s) =>
        s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();

    private static IReadOnlyDictionary<string, uint> BuildKeyTable()
    {
        var table = new Dictionary<string, uint>();
        for (var c = 'A'; c <= 'Z'; c++)
        {
            table[c.ToString()] = c; // VK codes for A-Z match their ASCII value.
        }

        for (var d = '0'; d <= '9'; d++)
        {
            table[d.ToString()] = d; // VK codes for 0-9 match their ASCII value.
        }

        for (var f = 1; f <= 12; f++)
        {
            table[$"F{f}"] = (uint)(0x70 + f - 1); // VK_F1 = 0x70.
        }

        table["Tab"] = 0x09;
        table["Space"] = 0x20;
        table["Escape"] = 0x1B;
        table["Enter"] = 0x0D;
        return table;
    }
}
