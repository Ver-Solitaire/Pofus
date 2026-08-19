namespace Pofus.Core.Craft;

/// <summary>
/// A workshop import could not be completed. Always carries a message meant to
/// be shown as-is to the user, so no failure ever surfaces as a crash or as an
/// unexplained empty list (Principe I).
/// </summary>
public sealed class CraftDataUnavailableException : Exception
{
    public CraftDataUnavailableException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}
