using System.Windows;
using System.Windows.Controls;
using Pofus.Core.Craft;
using Pofus.Core.Logging;
using Pofus.Platform;

namespace Pofus.Hud.Modules.Craft;

/// <summary>Hosted in the HUD's "group-actions" slot — opens the crafting shopping list.</summary>
public sealed class CraftHudModule : IHudModule
{
    private readonly ICraftStateStore _stateStore;
    private readonly IAppLogger _logger;
    private readonly ITopmostWindowController _topmostController;
    private readonly ForegroundWindowWatcher _foregroundWatcher;
    private readonly IWin32WindowApi _win32Api;
    private CraftWindow? _window;

    public CraftHudModule(
        ICraftStateStore stateStore,
        IAppLogger logger,
        ITopmostWindowController topmostController,
        ForegroundWindowWatcher foregroundWatcher,
        IWin32WindowApi win32Api)
    {
        _stateStore = stateStore;
        _logger = logger;
        _topmostController = topmostController;
        _foregroundWatcher = foregroundWatcher;
        _win32Api = win32Api;
    }

    public string ModuleId => "group-actions";

    public string DisplayName => "Atelier";

    // No real icon asset yet (deferred — see specs/001 tasks.md T027).
    public Uri IconResource { get; } =
        new("pack://application:,,,/Pofus.Hud;component/Assets/Icons/craft.png");

    public bool IsAvailable => true;

    public UIElement CreateContent()
    {
        var button = new Button
        {
            Content = "Craft",
            Padding = new Thickness(2),
            FontSize = 8,
            ToolTip = "Atelier — liste de ressources",
        };
        button.Click += (_, _) => OpenWindow();
        return button;
    }

    public void OnActivated()
    {
    }

    public void OnDeactivated()
    {
        _window?.Close();
        _window = null;
    }

    private void OpenWindow()
    {
        // Tested against null, not IsLoaded: WPF leaves IsLoaded true after a
        // window is closed, so the old check called Activate() on a dead window
        // and the atelier could never be reopened once shut.
        if (_window is not null)
        {
            _window.Activate();
            return;
        }

        _window = new CraftWindow(_stateStore, _logger, _topmostController, _foregroundWatcher, _win32Api);
        _window.Closed += (_, _) => _window = null;
        _window.Show();
    }
}
