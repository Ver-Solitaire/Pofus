using Pofus.Core.Accounts;

namespace Pofus.Core.Navigation;

/// <summary>
/// Pure computation of which window to activate next, given the already
/// filtered/ordered list of active accounts (feature 002) and the window
/// currently in the foreground. No Win32 dependency — the actual activation
/// is Pofus.Platform's job (Principe III).
/// </summary>
public static class WindowCycleNavigator
{
    public static nint? GetNext(IReadOnlyList<AccountSession> activeSessions, nint? currentWindowHandle) =>
        Cycle(activeSessions, currentWindowHandle, +1);

    public static nint? GetPrevious(IReadOnlyList<AccountSession> activeSessions, nint? currentWindowHandle) =>
        Cycle(activeSessions, currentWindowHandle, -1);

    /// <summary>Returns null if no account is currently marked leader (FR-008/FR-009).</summary>
    public static nint? GetLeader(IReadOnlyList<AccountSession> activeSessions)
    {
        foreach (var session in activeSessions)
        {
            if (session.IsLeader)
            {
                return session.WindowHandle;
            }
        }

        return null;
    }

    private static nint? Cycle(IReadOnlyList<AccountSession> sessions, nint? currentWindowHandle, int direction)
    {
        if (sessions.Count == 0)
        {
            return null;
        }

        var currentIndex = currentWindowHandle.HasValue
            ? IndexOf(sessions, currentWindowHandle.Value)
            : -1;

        // If the foreground window isn't one of the known active accounts
        // (e.g. Pofus itself, or an unrelated app), "next" starts at the
        // first account and "previous" starts at the last one.
        var baseIndex = currentIndex >= 0 ? currentIndex : (direction > 0 ? -1 : 0);
        var nextIndex = ((baseIndex + direction) % sessions.Count + sessions.Count) % sessions.Count;
        return sessions[nextIndex].WindowHandle;
    }

    private static int IndexOf(IReadOnlyList<AccountSession> sessions, nint handle)
    {
        for (var i = 0; i < sessions.Count; i++)
        {
            if (sessions[i].WindowHandle == handle)
            {
                return i;
            }
        }

        return -1;
    }
}
