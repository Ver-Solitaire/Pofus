using System.Windows;
using System.Windows.Threading;
using Pofus.Core.Accounts;

namespace Pofus.Hud.Modules.Accounts;

/// <summary>
/// Dedicated account manager opened from the HUD's "accounts" slot — mirrors
/// the reference project's "gestionnaire de personnages dédié" rather than
/// squeezing the full list into a 40×40 HUD slot (research.md).
/// </summary>
public partial class AccountManagerWindow : Window
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(2);

    private readonly IAccountDetectionService _detectionService;
    private readonly IAccountPreferencesStore _preferencesStore;
    private readonly DispatcherTimer _refreshTimer;
    private readonly Dictionary<string, AccountRowView> _rowsByPseudo = new(StringComparer.OrdinalIgnoreCase);
    private List<string> _displayedOrder = [];

    public AccountManagerWindow(IAccountDetectionService detectionService, IAccountPreferencesStore preferencesStore)
    {
        InitializeComponent();
        _detectionService = detectionService;
        _preferencesStore = preferencesStore;

        _refreshTimer = new DispatcherTimer { Interval = RefreshInterval };
        _refreshTimer.Tick += async (_, _) => await RefreshAsync();

        Loaded += async (_, _) =>
        {
            await RefreshAsync();
            _refreshTimer.Start();
        };
        Closed += (_, _) => _refreshTimer.Stop();
    }

    private void OnHeaderMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) => DragMove();

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private async Task RefreshAsync()
    {
        var accounts = await _detectionService.RefreshAsync();

        if (accounts.Count == 0)
        {
            EmptyStateText.Visibility = Visibility.Visible;
            RowsPanel.Visibility = Visibility.Collapsed;
            RowsPanel.Children.Clear();
            _rowsByPseudo.Clear();
            _displayedOrder = [];
            return;
        }

        EmptyStateText.Visibility = Visibility.Collapsed;
        RowsPanel.Visibility = Visibility.Visible;

        var newOrder = accounts.Select(a => a.Pseudo).ToList();
        if (!newOrder.SequenceEqual(_displayedOrder, StringComparer.OrdinalIgnoreCase))
        {
            RowsPanel.Children.Clear();
            _rowsByPseudo.Clear();
            foreach (var account in accounts)
            {
                var row = CreateRow(account);
                _rowsByPseudo[account.Pseudo] = row;
                RowsPanel.Children.Add(row);
            }

            _displayedOrder = newOrder;
            return;
        }

        foreach (var account in accounts)
        {
            var row = _rowsByPseudo[account.Pseudo];
            if (!row.IsKeyboardFocusWithin)
            {
                row.Bind(account);
            }
        }
    }

    private AccountRowView CreateRow(AccountSession account)
    {
        var row = new AccountRowView();
        row.Bind(account);
        row.ActiveToggled += OnActiveToggled;
        row.TeamChanged += OnTeamChanged;
        row.LeaderRequested += OnLeaderRequested;
        row.LeaderCleared += OnLeaderCleared;
        row.MoveUpRequested += OnMoveUpRequested;
        row.MoveDownRequested += OnMoveDownRequested;
        return row;
    }

    private async void OnActiveToggled(string pseudo, bool isActive)
    {
        var preferences = await _preferencesStore.LoadAsync();
        preferences.ActiveByPseudo[pseudo] = isActive;
        await _preferencesStore.SaveAsync(preferences);
    }

    private async void OnTeamChanged(string pseudo, string team)
    {
        var preferences = await _preferencesStore.LoadAsync();
        preferences.TeamByPseudo[pseudo] = team;
        await _preferencesStore.SaveAsync(preferences);
    }

    private async void OnLeaderRequested(string pseudo)
    {
        var preferences = await _preferencesStore.LoadAsync();
        preferences.LeaderPseudo = pseudo;
        await _preferencesStore.SaveAsync(preferences);
        ApplyLeaderToRows(pseudo);
    }

    private async void OnLeaderCleared(string pseudo)
    {
        var preferences = await _preferencesStore.LoadAsync();
        if (preferences.LeaderPseudo == pseudo)
        {
            preferences.LeaderPseudo = null;
            await _preferencesStore.SaveAsync(preferences);
        }

        ApplyLeaderToRows(null);
    }

    private void ApplyLeaderToRows(string? leaderPseudo)
    {
        foreach (var (pseudo, row) in _rowsByPseudo)
        {
            row.SetLeaderState(string.Equals(pseudo, leaderPseudo, StringComparison.OrdinalIgnoreCase));
        }
    }

    private async void OnMoveUpRequested(string pseudo) => await MoveAsync(pseudo, -1);

    private async void OnMoveDownRequested(string pseudo) => await MoveAsync(pseudo, 1);

    /// <summary>
    /// Reorders within the currently displayed (detected) accounts only, then
    /// maps that new relative order back onto the positions those pseudos
    /// occupy in the full CustomOrder — pseudos not currently displayed keep
    /// their slot untouched. Mirrors the reference project's
    /// move_account/_update_global_order_from_active.
    /// </summary>
    private async Task MoveAsync(string pseudo, int direction)
    {
        var currentIndex = _displayedOrder.FindIndex(
            p => string.Equals(p, pseudo, StringComparison.OrdinalIgnoreCase));
        if (currentIndex < 0)
        {
            return;
        }

        var newIndex = currentIndex + direction;
        if (newIndex < 0 || newIndex >= _displayedOrder.Count)
        {
            return;
        }

        var reordered = new List<string>(_displayedOrder);
        (reordered[currentIndex], reordered[newIndex]) = (reordered[newIndex], reordered[currentIndex]);

        var preferences = await _preferencesStore.LoadAsync();
        ApplyDisplayedOrder(preferences.CustomOrder, reordered);
        await _preferencesStore.SaveAsync(preferences);

        await RefreshAsync();
    }

    private static void ApplyDisplayedOrder(List<string> customOrder, List<string> newDisplayedOrder)
    {
        var displayedSet = new HashSet<string>(newDisplayedOrder, StringComparer.OrdinalIgnoreCase);
        var slots = customOrder
            .Select((pseudo, index) => (pseudo, index))
            .Where(x => displayedSet.Contains(x.pseudo))
            .Select(x => x.index)
            .ToList();

        for (var i = 0; i < slots.Count && i < newDisplayedOrder.Count; i++)
        {
            customOrder[slots[i]] = newDisplayedOrder[i];
        }
    }
}
