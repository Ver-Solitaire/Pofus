using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Pofus.Core.Models;
using Pofus.Core.Persistence;
using Pofus.Hud.Controls;
using Pofus.Platform;

namespace Pofus.Hud;

/// <summary>
/// The single, shared HUD window. Stays above every open Dofus window at once —
/// it is not anchored to whichever one currently has focus (FR-001, FR-005).
/// Which account currently has focus is shown by the separate, detachable
/// <see cref="Switcher.AccountSwitcherWidget"/>, not this window.
/// </summary>
public partial class HudWindow : Window
{
    private static readonly TimeSpan PositionSaveDebounce = TimeSpan.FromMilliseconds(500);

    private readonly ITopmostWindowController _topmostController;
    private readonly ForegroundWindowWatcher _foregroundWatcher;
    private readonly IHudLayoutStore _layoutStore;
    private readonly HudLayout _layout;
    private readonly IReadOnlyDictionary<string, IHudModule> _modulesBySlotId;
    private readonly ModuleHost _moduleHost;
    private readonly IWin32WindowApi _win32Api;
    private readonly uint _ownProcessId = (uint)Environment.ProcessId;
    private readonly DispatcherTimer _positionSaveTimer;
    private nint _selfHandle;

    public HudWindow(
        ITopmostWindowController topmostController,
        ForegroundWindowWatcher foregroundWatcher,
        IHudLayoutStore layoutStore,
        HudLayout layout,
        IReadOnlyDictionary<string, IHudModule> modulesBySlotId,
        ModuleHost moduleHost,
        IWin32WindowApi win32Api)
    {
        InitializeComponent();

        _win32Api = win32Api;
        _topmostController = topmostController;
        _foregroundWatcher = foregroundWatcher;
        _layoutStore = layoutStore;
        _layout = layout;
        _modulesBySlotId = modulesBySlotId;
        _moduleHost = moduleHost;

        _positionSaveTimer = new DispatcherTimer { Interval = PositionSaveDebounce };
        _positionSaveTimer.Tick += OnPositionSaveTimerTick;

        Left = _layout.WindowPosition.X;
        Top = _layout.WindowPosition.Y;

        RenderModuleSlots();

        Loaded += OnLoaded;
        Closing += OnClosing;
        Closed += OnClosed;
    }

    private void RenderModuleSlots()
    {
        ModuleSlotsPanel.Children.Clear();
        foreach (var slot in _layout.Slots.OrderBy(s => s.Position))
        {
            UIElement? moduleContent = null;
            if (_modulesBySlotId.TryGetValue(slot.SlotId, out var module))
            {
                // Modules are created/activated even while hidden: hiding is a
                // presentation choice, the module keeps running (FR-003).
                moduleContent = _moduleHost.TryCreateContent(module);
                if (moduleContent is not null)
                {
                    _moduleHost.TryActivate(module);
                }
            }

            slot.State = moduleContent is not null ? ModuleSlotState.Occupied : ModuleSlotState.Empty;

            var slotView = new ModuleSlotView
            {
                SlotLabel = slot.SlotId,
                Margin = new Thickness(4, 0, 0, 0),
                State = slot.State,
            };

            if (moduleContent is not null)
            {
                slotView.SetModuleContent(moduleContent);
            }

            ModuleSlotsPanel.Children.Add(slotView);
        }
    }

    private void SchedulePersist()
    {
        _positionSaveTimer.Stop();
        _positionSaveTimer.Start();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _selfHandle = new WindowInteropHelper(this).Handle;
        _topmostController.BringToTop(_selfHandle);

        // Reassert the persisted position now that the window/HWND fully exists —
        // AllowsTransparency windows can otherwise settle at an OS-chosen spot
        // instead of the Left/Top requested before Show(). Only start listening
        // for LocationChanged after this so we don't mistake that settling for a
        // genuine user drag and persist the wrong coordinates.
        Left = _layout.WindowPosition.X;
        Top = _layout.WindowPosition.Y;
        LocationChanged += OnLocationChanged;

        _foregroundWatcher.ForegroundWindowChanged += OnForegroundWindowChanged;
        _foregroundWatcher.Start();
    }

    private void OnForegroundWindowChanged(nint newForegroundWindow)
    {
        // Ignore our own windows — not just this one. A ContextMenu or any
        // other popup we open is a separate HWND that takes the foreground;
        // re-asserting HWND_TOPMOST here would raise the HUD above that popup
        // and hide it (found while validating feature 005).
        if (newForegroundWindow == _selfHandle || IsOwnProcessWindow(newForegroundWindow))
        {
            return;
        }

        // SetWinEventHook callbacks arrive off the WPF dispatcher thread.
        Dispatcher.Invoke(() => _topmostController.BringToTop(_selfHandle));
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

    private void OnRootPanelMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Moves the whole HUD window; individual slot positions stay fixed (FR-008).
        DragMove();
    }

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        _layout.WindowPosition.X = Left;
        _layout.WindowPosition.Y = Top;

        SchedulePersist();
    }

    private async void OnPositionSaveTimerTick(object? sender, EventArgs e)
    {
        _positionSaveTimer.Stop();
        await _layoutStore.SaveAsync(_layout);
    }

    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _positionSaveTimer.Stop();
        await _layoutStore.SaveAsync(_layout);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _foregroundWatcher.ForegroundWindowChanged -= OnForegroundWindowChanged;
        _foregroundWatcher.Dispose();
    }
}
