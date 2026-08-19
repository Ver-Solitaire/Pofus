using System.Windows;
using System.Windows.Controls;
using Pofus.Core.Accounts;

namespace Pofus.Hud.Modules.Accounts;

/// <summary>
/// First concrete implementation of <see cref="IHudModule"/> (contract shipped
/// empty in feature 001). Renders a compact indicator in the "accounts" slot
/// and opens the dedicated <see cref="AccountManagerWindow"/> on click.
/// </summary>
public sealed class AccountsHudModule : IHudModule
{
    private readonly IAccountDetectionService _detectionService;
    private readonly IAccountPreferencesStore _preferencesStore;
    private AccountManagerWindow? _managerWindow;

    public AccountsHudModule(IAccountDetectionService detectionService, IAccountPreferencesStore preferencesStore)
    {
        _detectionService = detectionService;
        _preferencesStore = preferencesStore;
    }

    public string ModuleId => "accounts";

    public string DisplayName => "Comptes";

    // No real icon asset yet (deferred — see feature 001 tasks.md T027); the
    // slot content below is a text badge, not an <Image>, so this Uri is not
    // actually dereferenced today.
    public Uri IconResource { get; } =
        new("pack://application:,,,/Pofus.Hud;component/Assets/Icons/accounts.png");

    public bool IsAvailable => true;

    public UIElement CreateContent()
    {
        var button = new Button
        {
            Content = "Comptes",
            Padding = new Thickness(2),
            FontSize = 8,
            ToolTip = DisplayName,
        };
        button.Click += (_, _) => OpenManager();
        return button;
    }

    public void OnActivated()
    {
    }

    public void OnDeactivated()
    {
        _managerWindow?.Close();
        _managerWindow = null;
    }

    private void OpenManager()
    {
        if (_managerWindow is { IsLoaded: true })
        {
            _managerWindow.Activate();
            return;
        }

        _managerWindow = new AccountManagerWindow(_detectionService, _preferencesStore);
        _managerWindow.Show();
    }
}
