using System.Windows.Controls;
using Pofus.Core.Accounts;

namespace Pofus.Hud.Modules.Accounts;

/// <summary>One row of the account manager: toggle, leader star, pseudo, class, team, reorder.</summary>
public partial class AccountRowView : UserControl
{
    private bool _suppressEvents;

    public event Action<string, bool>? ActiveToggled;
    public event Action<string, string>? TeamChanged;
    public event Action<string>? LeaderRequested;
    public event Action<string>? LeaderCleared;
    public event Action<string>? MoveUpRequested;
    public event Action<string>? MoveDownRequested;

    public AccountRowView()
    {
        InitializeComponent();
    }

    public string Pseudo { get; private set; } = string.Empty;

    public void Bind(AccountSession session)
    {
        _suppressEvents = true;
        Pseudo = session.Pseudo;
        PseudoText.Text = session.Pseudo;
        ClassNameText.Text = session.ClassName;
        ActiveCheckBox.IsChecked = session.IsActive;
        TeamTextBox.Text = session.Team;
        LeaderToggle.IsChecked = session.IsLeader;
        _suppressEvents = false;
    }

    /// <summary>Updates only the leader star, without touching other fields
    /// (e.g. while the user is mid-edit in the team textbox of this row).</summary>
    public void SetLeaderState(bool isLeader)
    {
        _suppressEvents = true;
        LeaderToggle.IsChecked = isLeader;
        _suppressEvents = false;
    }

    private void OnActiveChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_suppressEvents)
        {
            return;
        }

        ActiveToggled?.Invoke(Pseudo, ActiveCheckBox.IsChecked == true);
    }

    private void OnLeaderChecked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_suppressEvents)
        {
            return;
        }

        LeaderRequested?.Invoke(Pseudo);
    }

    private void OnLeaderUnchecked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_suppressEvents)
        {
            return;
        }

        LeaderCleared?.Invoke(Pseudo);
    }

    private void OnTeamLostFocus(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_suppressEvents)
        {
            return;
        }

        var team = string.IsNullOrWhiteSpace(TeamTextBox.Text)
            ? AccountPreferences.DefaultTeamName
            : TeamTextBox.Text.Trim();
        TeamChanged?.Invoke(Pseudo, team);
    }

    private void OnMoveUpClick(object sender, System.Windows.RoutedEventArgs e) => MoveUpRequested?.Invoke(Pseudo);

    private void OnMoveDownClick(object sender, System.Windows.RoutedEventArgs e) => MoveDownRequested?.Invoke(Pseudo);
}
