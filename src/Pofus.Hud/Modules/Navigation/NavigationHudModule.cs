using System.Windows;
using System.Windows.Controls;
using Pofus.Core.Accounts;
using Pofus.Core.Logging;
using Pofus.Core.Navigation;
using Pofus.Hud.Panels;
using Pofus.Platform;

namespace Pofus.Hud.Modules.Navigation;

/// <summary>
/// Hosted in the HUD's "macros" slot — the first automation capability
/// delivered there (window navigation), more to follow later in the same
/// spirit as "accounts" (feature 002). Opens
/// <see cref="NavigationShortcutsWindow"/> to reconfigure bindings.
/// </summary>
public sealed class NavigationHudModule : IHudModule
{
    private readonly IAccountDetectionService _accountDetectionService;
    private readonly INavigationShortcutStore _shortcutStore;
    private readonly IWindowActivator _windowActivator;
    private readonly IWin32WindowApi _win32Api;
    private readonly IAppLogger _logger;
    // Resolved lazily: the service registers windows that cannot exist yet
    // when this module is constructed (the HUD takes the module list).
    private readonly Func<PanelVisibilityService> _panels;
    private GlobalHotkeyListener? _hotkeyListener;
    private NavigationShortcutsWindow? _shortcutsWindow;

    public NavigationHudModule(
        IAccountDetectionService accountDetectionService,
        INavigationShortcutStore shortcutStore,
        IWindowActivator windowActivator,
        IWin32WindowApi win32Api,
        IAppLogger logger,
        Func<PanelVisibilityService> panels)
    {
        _accountDetectionService = accountDetectionService;
        _shortcutStore = shortcutStore;
        _windowActivator = windowActivator;
        _win32Api = win32Api;
        _logger = logger;
        _panels = panels;
    }

    public string ModuleId => "macros";

    public string DisplayName => "Navigation";

    // No real icon asset yet (deferred — see feature 001 tasks.md T027).
    public Uri IconResource { get; } =
        new("pack://application:,,,/Pofus.Hud;component/Assets/Icons/navigation.png");

    public bool IsAvailable => true;

    public UIElement CreateContent()
    {
        var button = new Button
        {
            Content = "Nav",
            Padding = new Thickness(2),
            FontSize = 8,
            ToolTip = "Navigation entre comptes (raccourcis clavier)",
        };
        button.Click += (_, _) => OpenShortcutsWindow();
        return button;
    }

    public void OnActivated()
    {
    }

    public void OnDeactivated()
    {
        _hotkeyListener?.Dispose();
        _hotkeyListener = null;
        _shortcutsWindow?.Close();
        _shortcutsWindow = null;
    }

    /// <summary>
    /// Called once the host HUD window is Loaded (real HWND available) —
    /// registers the persisted (or default) hotkey bindings.
    /// </summary>
    public async void AttachHotkeyListener(GlobalHotkeyListener listener)
    {
        _hotkeyListener = listener;
        listener.HotkeyPressed += OnHotkeyPressed;

        var preferences = await _shortcutStore.LoadAsync();
        foreach (var (action, combo) in preferences.Bindings)
        {
            listener.RegisterOrReplace(NavigationActionIds.ToActionId(action), combo);
        }
    }

    private void OnHotkeyPressed(string actionId)
    {
        // The listener is shared with feature 005's "module:*" un-hide
        // actions — ignore anything that isn't ours.
        if (NavigationActionIds.TryParse(actionId, out var action))
        {
            OnNavigationRequested(action);
        }
    }

    private async void OnNavigationRequested(NavigationAction action)
    {
        var sessions = await _accountDetectionService.RefreshAsync();
        var activeSessions = sessions.Where(s => s.IsActive).ToList();
        var currentForeground = _win32Api.GetForegroundWindow();

        nint? target = action switch
        {
            NavigationAction.Next => WindowCycleNavigator.GetNext(activeSessions, currentForeground),
            NavigationAction.Previous => WindowCycleNavigator.GetPrevious(activeSessions, currentForeground),
            NavigationAction.GoToLeader => WindowCycleNavigator.GetLeader(activeSessions),
            _ => null,
        };

        if (target is null)
        {
            // No active accounts (or no leader for GoToLeader) — explicit no-op, not an error (FR-008).
            _logger.LogInfo($"Navigation action {action} had no target window.");
            return;
        }

        var handle = target.Value;
        // WindowActivator blocks its thread for up to ~300ms while confirming
        // activation — must stay off the UI thread (Principe II).
        await Task.Run(() => _windowActivator.TryActivate(handle));
    }

    private void OpenShortcutsWindow()
    {
        if (_shortcutsWindow is { IsLoaded: true })
        {
            _shortcutsWindow.Activate();
            return;
        }

        if (_hotkeyListener is null)
        {
            _logger.LogWarning("Cannot open shortcuts window before hotkeys are attached.");
            return;
        }

        _shortcutsWindow = new NavigationShortcutsWindow(
            _shortcutStore, _hotkeyListener, _panels());
        _shortcutsWindow.Show();
    }
}
