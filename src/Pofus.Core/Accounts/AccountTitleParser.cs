namespace Pofus.Core.Accounts;

public sealed record ParsedAccountTitle(string Pseudo, string ClassName);

/// <summary>
/// Extracts pseudo/class from a Dofus window title, ported from the reference
/// project's <c>"Pseudo - Classe"</c> convention (confirmed against a real
/// client: <c>"Mon-Perso - Ouginak - 3.6.10.10 - Release"</c>). Pure parsing,
/// no Win32 dependency — testable without a Windows desktop session.
/// </summary>
public static class AccountTitleParser
{
    public const string UnknownClassName = "Classe inconnue";

    /// <summary>
    /// Returns null for launcher/character-selection windows (empty title, or
    /// title starting with "dofus") — these are not playable accounts.
    /// </summary>
    public static ParsedAccountTitle? Parse(string windowTitle)
    {
        var trimmed = windowTitle.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith("dofus", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var parts = trimmed.Split(" - ", StringSplitOptions.None);
        var pseudo = parts[0].Trim();
        if (pseudo.Length == 0)
        {
            return null;
        }

        var className = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])
            ? parts[1].Trim()
            : UnknownClassName;

        return new ParsedAccountTitle(pseudo, className);
    }
}
