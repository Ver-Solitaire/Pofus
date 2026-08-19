using Pofus.Core.Logging;
using Pofus.Core.Platform;

namespace Pofus.Core.Accounts;

public interface IAccountDetectionService
{
    /// <summary>
    /// Detects currently open Dofus accounts and merges them with persisted
    /// preferences into an ordered list. Never throws.
    /// </summary>
    Task<IReadOnlyList<AccountSession>> RefreshAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Merges raw window detection (<see cref="IDofusWindowLocator"/>) with
/// <see cref="AccountTitleParser"/> and persisted <see cref="AccountPreferences"/>
/// into the list of accounts the UI displays. Also maintains
/// <see cref="AccountPreferences.CustomOrder"/>: new pseudos are appended,
/// and entries beyond <see cref="AccountPreferences.MaxCustomOrderEntries"/>
/// are purged, oldest not-currently-detected pseudo first (FR-010).
/// </summary>
public sealed class AccountDetectionService : IAccountDetectionService
{
    private readonly IDofusWindowLocator _windowLocator;
    private readonly IAccountPreferencesStore _preferencesStore;
    private readonly IAppLogger _logger;

    public AccountDetectionService(
        IDofusWindowLocator windowLocator, IAccountPreferencesStore preferencesStore, IAppLogger logger)
    {
        _windowLocator = windowLocator;
        _preferencesStore = preferencesStore;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AccountSession>> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var preferences = await _preferencesStore.LoadAsync(cancellationToken);
        var windows = _windowLocator.GetOpenDofusWindows();

        var seenPseudos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sessions = new List<AccountSession>();
        var orderChanged = false;

        foreach (var window in windows)
        {
            var parsed = AccountTitleParser.Parse(window.Title);
            if (parsed is null)
            {
                continue;
            }

            if (!seenPseudos.Add(parsed.Pseudo))
            {
                _logger.LogWarning(
                    $"Duplicate pseudo '{parsed.Pseudo}' detected across multiple windows; " +
                    "keeping the first one until the next refresh.");
                continue;
            }

            var isActive = preferences.ActiveByPseudo.GetValueOrDefault(parsed.Pseudo, true);
            var team = preferences.TeamByPseudo.GetValueOrDefault(parsed.Pseudo, AccountPreferences.DefaultTeamName);
            var isLeader = preferences.LeaderPseudo == parsed.Pseudo;

            sessions.Add(new AccountSession
            {
                Pseudo = parsed.Pseudo,
                ClassName = parsed.ClassName,
                WindowHandle = window.Handle,
                IsActive = isActive,
                Team = team,
                IsLeader = isLeader,
            });

            if (!preferences.CustomOrder.Contains(parsed.Pseudo))
            {
                preferences.CustomOrder.Add(parsed.Pseudo);
                orderChanged = true;
            }
        }

        orderChanged |= PurgeStaleOrderEntries(preferences, seenPseudos);
        if (orderChanged)
        {
            await _preferencesStore.SaveAsync(preferences, cancellationToken);
        }

        var orderIndex = preferences.CustomOrder
            .Select((pseudo, index) => (pseudo, index))
            .ToDictionary(x => x.pseudo, x => x.index, StringComparer.OrdinalIgnoreCase);

        return sessions
            .OrderBy(s => orderIndex.GetValueOrDefault(s.Pseudo, int.MaxValue))
            .ToList();
    }

    private static bool PurgeStaleOrderEntries(AccountPreferences preferences, HashSet<string> currentlyDetected)
    {
        if (preferences.CustomOrder.Count <= AccountPreferences.MaxCustomOrderEntries)
        {
            return false;
        }

        var excess = preferences.CustomOrder.Count - AccountPreferences.MaxCustomOrderEntries;
        var toRemove = preferences.CustomOrder
            .Where(pseudo => !currentlyDetected.Contains(pseudo))
            .Take(excess)
            .ToList();

        foreach (var pseudo in toRemove)
        {
            preferences.CustomOrder.Remove(pseudo);
        }

        return toRemove.Count > 0;
    }
}
