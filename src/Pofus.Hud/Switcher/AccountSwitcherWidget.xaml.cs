using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Pofus.Core.Accounts;
using Pofus.Core.Switcher;
using Pofus.Platform;

namespace Pofus.Hud.Switcher;

/// <summary>
/// Detachable bar showing every detected Dofus account as a class-colored
/// badge, with the one currently in the foreground ringed in white —
/// independent of the main HUD's position, freely draggable anywhere on screen.
/// </summary>
public partial class AccountSwitcherWidget : Window
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan PositionSaveDebounce = TimeSpan.FromMilliseconds(500);

    private readonly IAccountDetectionService _detectionService;
    private readonly ITopmostWindowController _topmostController;
    private readonly ForegroundWindowWatcher _foregroundWatcher;
    private readonly IWindowActivator _windowActivator;
    private readonly IWin32WindowApi _win32Api;
    private readonly ISwitcherWidgetStore _widgetStore;
    private readonly SwitcherWidgetPreferences _preferences;
    private readonly WindowIconLoader _iconLoader;
    private readonly DispatcherTimer _refreshTimer;
    private readonly DispatcherTimer _positionSaveTimer;
    private readonly Dictionary<nint, AccountChipView> _chipsByHandle = [];
    private readonly uint _ownProcessId = (uint)Environment.ProcessId;
    private nint _selfHandle;
    private nint _currentForeground;

    public AccountSwitcherWidget(
        IAccountDetectionService detectionService,
        ITopmostWindowController topmostController,
        ForegroundWindowWatcher foregroundWatcher,
        IWindowActivator windowActivator,
        IWin32WindowApi win32Api,
        ISwitcherWidgetStore widgetStore,
        SwitcherWidgetPreferences preferences,
        WindowIconLoader iconLoader)
    {
        InitializeComponent();

        _iconLoader = iconLoader;

        _detectionService = detectionService;
        _topmostController = topmostController;
        _foregroundWatcher = foregroundWatcher;
        _windowActivator = windowActivator;
        _win32Api = win32Api;
        _widgetStore = widgetStore;
        _preferences = preferences;

        Left = _preferences.Position.X;
        Top = _preferences.Position.Y;

        _refreshTimer = new DispatcherTimer { Interval = RefreshInterval };
        _refreshTimer.Tick += async (_, _) => await RefreshAsync();

        _positionSaveTimer = new DispatcherTimer { Interval = PositionSaveDebounce };
        _positionSaveTimer.Tick += OnPositionSaveTimerTick;

        Loaded += OnLoaded;
        Closing += OnClosing;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _selfHandle = new WindowInteropHelper(this).Handle;
        _topmostController.BringToTop(_selfHandle);

        // Same settling issue as HudWindow (feature 001) — reassert the
        // persisted position now that the HWND fully exists.
        Left = _preferences.Position.X;
        Top = _preferences.Position.Y;
        LocationChanged += OnLocationChanged;

        _foregroundWatcher.ForegroundWindowChanged += OnForegroundWindowChanged;
        _foregroundWatcher.Start();

        _currentForeground = _win32Api.GetForegroundWindow();
        await RefreshAsync();
        _refreshTimer.Start();
    }

    private void OnForegroundWindowChanged(nint newForegroundWindow)
    {
        // Skip our own windows, popups included: re-asserting topmost while a
        // Pofus popup (e.g. a HUD slot's context menu) is open would raise this
        // widget above it and hide it.
        if (newForegroundWindow == _selfHandle || IsOwnProcessWindow(newForegroundWindow))
        {
            return;
        }

        Dispatcher.Invoke(() =>
        {
            _topmostController.BringToTop(_selfHandle);
            _currentForeground = newForegroundWindow;
            ApplyFocusHighlight();
        });
    }

    private async Task RefreshAsync()
    {
        var accounts = await _detectionService.RefreshAsync();

        if (accounts.Count == 0)
        {
            EmptyStateText.Visibility = Visibility.Visible;
            ChipsPanel.Visibility = Visibility.Collapsed;
            ChipsPanel.Children.Clear();
            _chipsByHandle.Clear();
            return;
        }

        EmptyStateText.Visibility = Visibility.Collapsed;
        ChipsPanel.Visibility = Visibility.Visible;

        ChipsPanel.Children.Clear();
        _chipsByHandle.Clear();
        foreach (var account in accounts)
        {
            var chip = new AccountChipView { Margin = new Thickness(4, 0, 0, 0) };
            chip.Bind(
                account.Pseudo, account.ClassName, account.WindowHandle, account.IsLeader,
                _iconLoader.TryLoad(account.WindowHandle));
            chip.Activated += OnChipActivated;
            ChipsPanel.Children.Add(chip);
            _chipsByHandle[account.WindowHandle] = chip;
        }

        ApplyFocusHighlight();
    }

    private void ApplyFocusHighlight()
    {
        foreach (var (handle, chip) in _chipsByHandle)
        {
            chip.SetFocused(handle == _currentForeground);
        }
    }

    private bool IsOwnProcessWindow(nint windowHandle)
    {
        if (windowHandle == nint.Zero)
        {
            return false;
        }

        _win32Api.GetWindowThreadProcessId(windowHandle, out var processId);
        return processId == _ownProcessId;
    }

    private async void OnChipActivated(nint windowHandle) =>
        await Task.Run(() => _windowActivator.TryActivate(windowHandle));

    private void OnRootPanelMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        _preferences.Position.X = Left;
        _preferences.Position.Y = Top;

        _positionSaveTimer.Stop();
        _positionSaveTimer.Start();
    }

    private async void OnPositionSaveTimerTick(object? sender, EventArgs e)
    {
        _positionSaveTimer.Stop();
        await _widgetStore.SaveAsync(_preferences);
    }

    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _positionSaveTimer.Stop();
        await _widgetStore.SaveAsync(_preferences);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _foregroundWatcher.ForegroundWindowChanged -= OnForegroundWindowChanged;
        _foregroundWatcher.Dispose();
    }
}
