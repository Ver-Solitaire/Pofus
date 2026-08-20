using System.Windows;
using System.Windows.Controls;
using Pofus.Core.Appearance;
using Pofus.Core.Settings;
using Pofus.Hud.Appearance;
using Pofus.Platform;

namespace Pofus.Hud.Modules.Settings;

/// <summary>Hosted in the HUD's "settings" slot — opens the general settings window.</summary>
public sealed class SettingsHudModule : IHudModule
{
    private readonly IAppPreferencesStore _preferencesStore;
    private readonly IStartupRegistration _startupRegistration;
    private readonly IAppearanceStore _appearanceStore;
    private readonly ThemeApplier _themeApplier;
    private readonly Func<AppearancePreferences> _currentAppearance;
    private SettingsWindow? _settingsWindow;

    public SettingsHudModule(
        IAppPreferencesStore preferencesStore,
        IStartupRegistration startupRegistration,
        IAppearanceStore appearanceStore,
        ThemeApplier themeApplier,
        Func<AppearancePreferences> currentAppearance)
    {
        _preferencesStore = preferencesStore;
        _startupRegistration = startupRegistration;
        _appearanceStore = appearanceStore;
        _themeApplier = themeApplier;
        _currentAppearance = currentAppearance;
    }

    public string ModuleId => "settings";

    public string DisplayName => "Réglages";

    // No real icon asset yet (deferred — see feature 001 tasks.md T027).
    public Uri IconResource { get; } =
        new("pack://application:,,,/Pofus.Hud;component/Assets/Icons/settings.png");

    public bool IsAvailable => true;

    public UIElement CreateContent()
    {
        var button = new Button
        {
            Content = "⚙",
            Padding = new Thickness(2),
            FontSize = 12,
            ToolTip = DisplayName,
        };
        button.Click += (_, _) => OpenSettingsWindow();
        return button;
    }

    public void OnActivated()
    {
    }

    public void OnDeactivated()
    {
        _settingsWindow?.Close();
        _settingsWindow = null;
    }

    private void OpenSettingsWindow()
    {
        // Tested against null, not IsLoaded: WPF leaves IsLoaded true after a
        // window is closed, so the old check reactivated a dead window and the
        // settings could never be reopened once shut.
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(
            _preferencesStore, _startupRegistration, _appearanceStore, _themeApplier, _currentAppearance());
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }
}
