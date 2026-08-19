namespace Pofus.Platform;

public interface IStartupRegistration
{
    bool IsEnabled();

    /// <summary>Returns true on success; on failure returns false and sets <paramref name="error"/>.</summary>
    bool TryEnable(out string? error);

    /// <summary>Returns true on success; on failure returns false and sets <paramref name="error"/>.</summary>
    bool TryDisable(out string? error);
}
