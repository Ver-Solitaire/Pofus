using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Pofus.Core.Appearance;
using Pofus.Core.Settings;
using Pofus.Hud.Appearance;
using Pofus.Platform;

namespace Pofus.Hud.Modules.Settings;

/// <summary>General settings window, opened from the HUD's "settings" slot.</summary>
public partial class SettingsWindow : Window
{
    private static readonly TimeSpan AppearanceSaveDebounce = TimeSpan.FromMilliseconds(400);

    private readonly IAppPreferencesStore _preferencesStore;
    private readonly IStartupRegistration _startupRegistration;
    private readonly IAppearanceStore _appearanceStore;
    private readonly ThemeApplier _themeApplier;
    private readonly DispatcherTimer _appearanceSaveTimer;
    private AppearancePreferences _appearance = AppearancePreferences.CreateDefault();
    private bool _suppressEvents;

    public SettingsWindow(
        IAppPreferencesStore preferencesStore,
        IStartupRegistration startupRegistration,
        IAppearanceStore appearanceStore,
        ThemeApplier themeApplier,
        AppearancePreferences appearance)
    {
        // Assigned BEFORE InitializeComponent: the opacity sliders declare
        // Minimum="0.15", so WPF raises ValueChanged while parsing the XAML —
        // the handlers must already have everything they need.
        _preferencesStore = preferencesStore;
        _startupRegistration = startupRegistration;
        _appearanceStore = appearanceStore;
        _themeApplier = themeApplier;
        _appearance = appearance;

        // ...and those parse-time events must not be mistaken for user edits.
        _suppressEvents = true;
        InitializeComponent();
        _suppressEvents = false;

        // Dragging a slider fires continuously — persist once the user settles,
        // not on every tick (Principe II).
        _appearanceSaveTimer = new DispatcherTimer { Interval = AppearanceSaveDebounce };
        _appearanceSaveTimer.Tick += OnAppearanceSaveTick;

        Loaded += (_, _) =>
        {
            RefreshLaunchAtStartupState();
            BuildPresetButtons();
            RefreshAppearanceControls();
        };
        Closed += (_, _) => _appearanceSaveTimer.Stop();
    }

    private void BuildPresetButtons()
    {
        PresetsPanel.Children.Clear();
        foreach (var preset in AppearancePresets.All)
        {
            var button = new Button { Content = preset.Name, Margin = new Thickness(0, 0, 6, 0) };
            button.Click += (_, _) => ApplyAppearance(preset.Values.Clone(), $"Préréglage « {preset.Name} » appliqué.");
            PresetsPanel.Children.Add(button);
        }
    }

    /// <summary>Pushes the model into the live theme, the controls and the disk.</summary>
    private void ApplyAppearance(AppearancePreferences appearance, string? status)
    {
        _appearance = appearance;
        _appearance.ClampOpacities();
        _themeApplier.Apply(_appearance);
        RefreshAppearanceControls();
        SchedulePersistAppearance();

        if (status is not null)
        {
            StatusText.Foreground = (Brush)FindResource("Pofus.Accent");
            StatusText.Text = status;
        }
    }

    private void RefreshAppearanceControls()
    {
        _suppressEvents = true;

        BackgroundHex.Text = _appearance.Background.ToHex();
        TextHex.Text = _appearance.Text.ToHex();
        BorderHex.Text = _appearance.Border.ToHex();

        BackgroundSwatch.Background = ToBrush(_appearance.Background);
        TextSwatch.Background = ToBrush(_appearance.Text);
        BorderSwatch.Background = ToBrush(_appearance.Border);

        BackgroundOpacitySlider.Value = _appearance.BackgroundOpacity;
        BorderOpacitySlider.Value = _appearance.BorderOpacity;
        BackgroundOpacityText.Text = $"{_appearance.BackgroundOpacity:P0}";
        BorderOpacityText.Text = $"{_appearance.BorderOpacity:P0}";

        _suppressEvents = false;
    }

    private static SolidColorBrush ToBrush(ThemeColor color) =>
        new(Color.FromRgb(color.R, color.G, color.B));

    private void OnBackgroundOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_suppressEvents)
        {
            return;
        }

        _appearance.BackgroundOpacity = e.NewValue;
        ApplyAppearance(_appearance, status: null);
    }

    private void OnBorderOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_suppressEvents)
        {
            return;
        }

        _appearance.BorderOpacity = e.NewValue;
        ApplyAppearance(_appearance, status: null);
    }

    private void OnBackgroundHexChanged(object sender, RoutedEventArgs e) =>
        ApplyHex(BackgroundHex.Text, c => _appearance.Background = c, "fond");

    private void OnTextHexChanged(object sender, RoutedEventArgs e) =>
        ApplyHex(TextHex.Text, c => _appearance.Text = c, "texte");

    private void OnBorderHexChanged(object sender, RoutedEventArgs e) =>
        ApplyHex(BorderHex.Text, c => _appearance.Border = c, "bordures");

    /// <summary>Enter validates a color field without waiting for focus to move.</summary>
    private void OnHexKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox box)
        {
            e.Handled = true;
            box.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }
    }

    private void ApplyHex(string text, Action<ThemeColor> assign, string label)
    {
        if (_suppressEvents)
        {
            return;
        }

        var parsed = ThemeColor.TryParseHex(text);
        if (parsed is null)
        {
            // Rejected, not silently ignored: tell the user and restore the
            // value that is actually in effect.
            StatusText.Foreground = (Brush)FindResource("Pofus.Danger");
            StatusText.Text = $"Couleur {label} invalide : utilisez le format #RRGGBB.";
            RefreshAppearanceControls();
            return;
        }

        assign(parsed.Value);
        ApplyAppearance(_appearance, status: null);
    }

    private void OnResetAppearanceClick(object sender, RoutedEventArgs e) =>
        ApplyAppearance(AppearancePreferences.CreateDefault(), "Apparence réinitialisée.");

    private void SchedulePersistAppearance()
    {
        _appearanceSaveTimer.Stop();
        _appearanceSaveTimer.Start();
    }

    private async void OnAppearanceSaveTick(object? sender, EventArgs e)
    {
        _appearanceSaveTimer.Stop();
        await _appearanceStore.SaveAsync(_appearance);
    }

    private void RefreshLaunchAtStartupState()
    {
        // The registry is the source of truth (data-model.md) — read it
        // directly rather than trusting only the persisted preference, in
        // case it was changed by another means.
        _suppressEvents = true;
        LaunchAtStartupCheckBox.IsChecked = _startupRegistration.IsEnabled();
        _suppressEvents = false;
    }

    private async void OnLaunchAtStartupChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressEvents)
        {
            return;
        }

        var wantEnabled = LaunchAtStartupCheckBox.IsChecked == true;
        var success = wantEnabled
            ? _startupRegistration.TryEnable(out var error)
            : _startupRegistration.TryDisable(out error);

        if (!success)
        {
            StatusText.Foreground = (Brush)FindResource("Pofus.Danger");
            StatusText.Text = $"Échec : {error}";
            RefreshLaunchAtStartupState();
            return;
        }

        var preferences = await _preferencesStore.LoadAsync();
        preferences.LaunchAtStartup = wantEnabled;
        await _preferencesStore.SaveAsync(preferences);

        StatusText.Foreground = (Brush)FindResource("Pofus.Accent");
        StatusText.Text = wantEnabled
            ? "Pofus démarrera automatiquement avec Windows."
            : "Lancement automatique désactivé.";
    }

    private async void OnResetConflictWarningClick(object sender, RoutedEventArgs e)
    {
        var preferences = await _preferencesStore.LoadAsync();
        preferences.IgnoreConflictWarning = false;
        await _preferencesStore.SaveAsync(preferences);

        StatusText.Foreground = (Brush)FindResource("Pofus.Accent");
        StatusText.Text = "L'avertissement de conflit logiciel réapparaîtra si détecté.";
    }

    private void OnHeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
