using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Pofus.Core.Navigation;
using Pofus.Hud.Panels;

namespace Pofus.Hud.Modules.Navigation;

/// <summary>
/// Lets the user re-assign each shortcut by pressing the new combination
/// directly (capture, not free-text) — eliminates a whole class of
/// typo/format errors (feature 003 research.md). Covers both the navigation
/// actions and, since feature 005, the per-window "un-hide" shortcuts, so
/// that conflicts across the two families are caught in one place.
/// </summary>
public partial class NavigationShortcutsWindow : Window
{
    private readonly INavigationShortcutStore _store;
    private readonly GlobalHotkeyListener _hotkeyListener;
    private readonly PanelVisibilityService _panels;
    private NavigationShortcutPreferences _preferences = new();
    private NavigationAction? _capturingAction;
    private string? _capturingPanelId;

    public NavigationShortcutsWindow(
        INavigationShortcutStore store,
        GlobalHotkeyListener hotkeyListener,
        PanelVisibilityService panels)
    {
        InitializeComponent();
        _store = store;
        _hotkeyListener = hotkeyListener;
        _panels = panels;

        PreviewKeyDown += OnPreviewKeyDown;
        // The extra mouse buttons are valid shortcuts too, so capture accepts
        // them alongside keys. Left and right are excluded: binding them would
        // make this very window unusable.
        PreviewMouseDown += OnPreviewMouseDown;
        Loaded += async (_, _) => await LoadAsync();
        // Never leave the hotkeys released if the window is closed mid-capture.
        Closed += (_, _) => _hotkeyListener.ResumeAll();
    }

    /// <summary>
    /// Enters capture mode: the global hotkeys are released so this window can
    /// actually see the keys pressed, including a combination that is already
    /// bound (otherwise Windows would deliver it as WM_HOTKEY and we could
    /// never report the conflict).
    /// </summary>
    private void BeginCaptureMode(string prompt)
    {
        _hotkeyListener.SuspendAll();
        StatusText.Text = prompt;
    }

    private void EndCaptureMode() => _hotkeyListener.ResumeAll();

    private void OnHeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        WindowPlacement.BeginDrag(this, e);

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private async Task LoadAsync()
    {
        _preferences = await _store.LoadAsync();
        RefreshBindingsDisplay();
        RefreshPanelRows();
    }

    /// <summary>
    /// Rebuilds one row per hideable window: its current binding, a capture
    /// button, and — for a currently hidden window — a direct "Réafficher"
    /// button, so a hidden window is never unreachable (FR-009).
    /// </summary>
    private void RefreshPanelRows()
    {
        PanelRowsPanel.Children.Clear();
        foreach (var panel in _panels.GetPanels())
        {
            PanelRowsPanel.Children.Add(BuildPanelRow(panel));
        }
    }

    private UIElement BuildPanelRow(HideablePanel panel)
    {
        var isHidden = _panels.IsHidden(panel.PanelId);
        var row = new Grid { Margin = new Thickness(0, 3, 0, 3) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var name = new TextBlock
        {
            Text = panel.DisplayName,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (System.Windows.Media.Brush)FindResource(
                isHidden ? "Pofus.TextMuted" : "Pofus.TextPrimary"),
        };
        Grid.SetColumn(name, 0);
        row.Children.Add(name);

        var combo = _panels.GetShortcut(panel.PanelId);
        var binding = new TextBlock
        {
            Text = combo?.ToDisplayString() ?? "aucun",
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (System.Windows.Media.Brush)FindResource("Pofus.TextMuted"),
        };
        Grid.SetColumn(binding, 1);
        row.Children.Add(binding);

        var modify = new Button { Content = "Modifier" };
        modify.Click += (_, _) => BeginPanelCapture(panel);
        Grid.SetColumn(modify, 2);
        row.Children.Add(modify);

        if (isHidden)
        {
            var show = new Button { Content = "Réafficher", Margin = new Thickness(6, 0, 0, 0) };
            show.Click += (_, _) =>
            {
                _panels.Show(panel.PanelId);
                RefreshPanelRows();
                StatusText.Text = $"« {panel.DisplayName} » est de nouveau affichée.";
            };
            Grid.SetColumn(show, 3);
            row.Children.Add(show);
        }

        return row;
    }

    private void BeginPanelCapture(HideablePanel panel)
    {
        _capturingAction = null;
        _capturingPanelId = panel.PanelId;
        BeginCaptureMode($"Appuyez sur la nouvelle combinaison pour réafficher « {panel.DisplayName} »...");
    }

    private void RefreshBindingsDisplay()
    {
        NextText.Text = _preferences.Bindings[NavigationAction.Next].ToDisplayString();
        PreviousText.Text = _preferences.Bindings[NavigationAction.Previous].ToDisplayString();
        LeaderText.Text = _preferences.Bindings[NavigationAction.GoToLeader].ToDisplayString();
    }

    private void OnModifyNextClick(object sender, RoutedEventArgs e) => BeginCapture(NavigationAction.Next);

    private void OnModifyPreviousClick(object sender, RoutedEventArgs e) => BeginCapture(NavigationAction.Previous);

    private void OnModifyLeaderClick(object sender, RoutedEventArgs e) => BeginCapture(NavigationAction.GoToLeader);

    private void BeginCapture(NavigationAction action)
    {
        _capturingPanelId = null;
        _capturingAction = action;
        BeginCaptureMode($"Appuyez sur la nouvelle combinaison pour « {DescribeAction(action)} »...");
    }

    private async void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_capturingAction is null && _capturingPanelId is null)
        {
            return;
        }

        var virtualKey = e.ChangedButton switch
        {
            System.Windows.Input.MouseButton.XButton1 => KeyCombo.VkExtraButton1,
            System.Windows.Input.MouseButton.XButton2 => KeyCombo.VkExtraButton2,
            System.Windows.Input.MouseButton.Middle => KeyCombo.VkMiddleButton,
            _ => 0u,
        };

        if (virtualKey == 0)
        {
            return; // left/right: let the click do its normal job
        }

        e.Handled = true;
        await ApplyCapturedComboAsync(KeyCombo.ForMouseButton(ToKeyModifiers(Keyboard.Modifiers), virtualKey));
    }

    private async void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_capturingAction is null && _capturingPanelId is null)
        {
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (IsModifierKey(key))
        {
            return; // wait for a non-modifier key to complete the combo
        }

        e.Handled = true;
        var combo = new KeyCombo(ToKeyModifiers(Keyboard.Modifiers), key.ToString(), (uint)KeyInterop.VirtualKeyFromKey(key));
        await ApplyCapturedComboAsync(combo);
    }

    /// <summary>Routes a captured combination to whichever binding is being edited.</summary>
    private async Task ApplyCapturedComboAsync(KeyCombo combo)
    {
        EndCaptureMode();

        if (_capturingAction is { } action)
        {
            _capturingAction = null;
            await ApplyNavigationBindingAsync(action, combo);
            return;
        }

        var panelId = _capturingPanelId!;
        _capturingPanelId = null;
        await ApplyPanelBindingAsync(panelId, combo);
    }

    private async Task ApplyNavigationBindingAsync(NavigationAction action, KeyCombo combo)
    {
        if (DescribeExistingUse(combo, excludingAction: action, excludingPanelId: null) is { } inUseBy)
        {
            StatusText.Text = $"Combinaison déjà utilisée par « {inUseBy} ».";
            return;
        }

        _preferences.Bindings[action] = combo;
        _hotkeyListener.RegisterOrReplace(NavigationActionIds.ToActionId(action), combo);
        await _store.SaveAsync(_preferences);

        RefreshBindingsDisplay();
        StatusText.Text = $"« {DescribeAction(action)} » assigné à {combo.ToDisplayString()}.";
    }

    private async Task ApplyPanelBindingAsync(string panelId, KeyCombo combo)
    {
        if (DescribeExistingUse(combo, excludingAction: null, excludingPanelId: panelId) is { } inUseBy)
        {
            StatusText.Text = $"Combinaison déjà utilisée par « {inUseBy} ».";
            return;
        }

        await _panels.SetShortcutAsync(panelId, combo);

        RefreshPanelRows();
        StatusText.Text =
            $"Réaffichage de « {_panels.DescribePanel(panelId)} » assigné à {combo.ToDisplayString()}.";
    }

    /// <summary>
    /// Names whatever already uses this combination, across BOTH the navigation
    /// and the window bindings — so a window shortcut can never silently
    /// displace a navigation one, or vice versa (FR-006).
    /// </summary>
    private string? DescribeExistingUse(
        KeyCombo combo, NavigationAction? excludingAction, string? excludingPanelId)
    {
        foreach (var (action, bound) in _preferences.Bindings)
        {
            if (action != excludingAction && bound == combo)
            {
                return DescribeAction(action);
            }
        }

        var panelConflict = _panels.FindShortcutConflict(combo, excludingPanelId ?? string.Empty);
        return panelConflict is null ? null : $"réafficher {_panels.DescribePanel(panelConflict)}";
    }

    private static bool IsModifierKey(Key key) => key is
        Key.LeftCtrl or Key.RightCtrl or
        Key.LeftAlt or Key.RightAlt or
        Key.LeftShift or Key.RightShift or
        Key.LWin or Key.RWin;

    private static KeyModifiers ToKeyModifiers(ModifierKeys modifiers)
    {
        var result = KeyModifiers.None;
        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            result |= KeyModifiers.Control;
        }

        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            result |= KeyModifiers.Alt;
        }

        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            result |= KeyModifiers.Shift;
        }

        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            result |= KeyModifiers.Win;
        }

        return result;
    }

    private static string DescribeAction(NavigationAction action) => action switch
    {
        NavigationAction.Next => "Compte suivant",
        NavigationAction.Previous => "Compte précédent",
        NavigationAction.GoToLeader => "Aller au leader",
        _ => action.ToString(),
    };
}
