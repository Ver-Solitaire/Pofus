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
        if (_settingsWindow is { IsLoaded: true })
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(
            _preferencesStore, _startupRegistration, _appearanceStore, _themeApplier, _currentAppearance());
        _settingsWindow.Show();
    }
}
