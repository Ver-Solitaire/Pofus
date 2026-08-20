using System.Windows;
using Pofus.Core.Accounts;
using Pofus.Core.Appearance;
using Pofus.Core.Craft;
using Pofus.Core.Logging;
using Pofus.Core.Models;
using Pofus.Core.Navigation;
using Pofus.Core.Panels;
using Pofus.Core.Persistence;
using Pofus.Core.Platform;
using Pofus.Core.Settings;
using Pofus.Core.Switcher;
using Pofus.Hud;
using Pofus.Hud.Appearance;
using Pofus.Hud.Modules.Accounts;
using Pofus.Hud.Modules.Craft;
using Pofus.Hud.Modules.Navigation;
using Pofus.Hud.Modules.Settings;
using Pofus.Hud.Panels;
using Pofus.Hud.Switcher;
using Pofus.Platform;
using Application = System.Windows.Application;
// The tray icon pulls in WinForms, which brings its own MessageBox.
using MessageBox = System.Windows.MessageBox;

namespace Pofus.App;

/// <summary>Composition root: wires Platform/Core services into the HUD and its companion windows.</summary>
public partial class App : Application
{
    private TrayIconController? _trayIconController;
    private ModuleHost? _moduleHost;
    private IReadOnlyDictionary<string, IHudModule>? _modulesBySlotId;
    private ForegroundWindowWatcher? _foregroundWatcher;
    private IAppLogger? _logger;
    private bool _crashNoticeShown;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        IAppLogger logger = new FileAppLogger();
        _logger = logger;

        // Installed first, so anything that fails below leaves a trace. Until
        // now an unhandled exception killed Pofus without writing a single line
        // to pofus.log — the crash was only visible in the Windows event log.
        InstallCrashHandlers(logger);
        WindowPlacement.Logger = logger;
        logger.LogInfo("Pofus démarre.");

        try
        {
            await StartAsync(logger);
        }
        catch (Exception ex)
        {
            // OnStartup is async void: past the first await, an exception here
            // would otherwise tear the process down mid-initialisation.
            logger.LogError("Le démarrage de Pofus a échoué.", ex);
            MessageBox.Show(
                "Pofus n'a pas pu démarrer.\n\n" +
                $"{ex.Message}\n\n" +
                "Le détail est dans %APPDATA%\\Pofus\\logs\\pofus.log.",
                "Pofus", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    /// <summary>
    /// Records every unhandled failure. A UI-thread exception no longer closes
    /// Pofus: a HUD that survives a glitch is worth more than one that vanishes
    /// mid-session, and the failure is logged and shown rather than swallowed
    /// (Constitution, Principe I).
    /// </summary>
    private void InstallCrashHandlers(IAppLogger logger)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            logger.LogError("Exception non gérée sur le thread d'interface.", args.Exception);
            args.Handled = true;
            ShowCrashNoticeOnce(args.Exception);
        };

        // Last resort: a failure on a background thread still terminates the
        // process, but at least the reason is written down.
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            logger.LogError(
                "Exception non gérée hors du thread d'interface — arrêt du processus.",
                args.ExceptionObject as Exception);

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            logger.LogError("Exception non observée dans une tâche en arrière-plan.", args.Exception);
            args.SetObserved();
        };
    }

    /// <summary>
    /// Warns once per session. A repeating fault would otherwise bury the screen
    /// under dialogs; the log keeps every occurrence.
    /// </summary>
    private void ShowCrashNoticeOnce(Exception exception)
    {
        if (_crashNoticeShown)
        {
            return;
        }

        _crashNoticeShown = true;
        MessageBox.Show(
            "Une erreur est survenue dans Pofus, qui continue de fonctionner.\n\n" +
            $"{exception.Message}\n\n" +
            "Le détail est dans %APPDATA%\\Pofus\\logs\\pofus.log.",
            "Pofus", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private async Task StartAsync(IAppLogger logger)
    {
        var win32Api = new Win32WindowApi();
        IDofusWindowLocator windowLocator = new DofusWindowLocator(logger, win32Api, new ProcessNameResolver(logger));
        ITopmostWindowController topmostController = new TopmostWindowController(logger, win32Api);
        IWindowActivator windowActivator = new WindowActivator(logger, win32Api);
        // Shared by HudWindow, AccountSwitcherWidget and every TopmostKeeper, so
        // only one Win32 event hook is installed for "which window is now in the
        // foreground". Owned here and disposed in OnExit: a window that disposed
        // it on close would unhook it for all the others.
        var foregroundWatcher = new ForegroundWindowWatcher(logger, win32Api);
        _foregroundWatcher = foregroundWatcher;

        IHudLayoutStore layoutStore = new HudLayoutStore(logger);
        var layout = await layoutStore.LoadAsync();
        if (layout.Slots.Count == 0)
        {
            layout = DefaultHudLayoutFactory.CreateDefault();
        }

        IAccountPreferencesStore accountPreferencesStore = new AccountPreferencesStore(logger);
        IAccountDetectionService accountDetectionService =
            new AccountDetectionService(windowLocator, accountPreferencesStore, logger);
        var accountsModule = new AccountsHudModule(accountDetectionService, accountPreferencesStore);

        INavigationShortcutStore navigationShortcutStore = new NavigationShortcutStore(logger);
        IPanelPreferencesStore panelPreferencesStore = new PanelPreferencesStore(logger);
        var panelVisibility = new PanelVisibilityService(panelPreferencesStore);
        var navigationModule = new NavigationHudModule(
            accountDetectionService, navigationShortcutStore, windowActivator, win32Api, logger,
            () => panelVisibility);

        IAppPreferencesStore appPreferencesStore = new AppPreferencesStore(logger);
        IProcessController processController = new ProcessController(logger);
        IConflictingSoftwareDetector conflictingSoftwareDetector = new ConflictingSoftwareDetector(processController);
        IStartupRegistration startupRegistration = new StartupRegistration(logger);

        // Appearance is applied before any window is shown, so nothing ever
        // flashes in the default theme first (feature 006).
        IAppearanceStore appearanceStore = new AppearanceStore(logger);
        var themeApplier = new ThemeApplier(Resources, logger);
        var appearance = await appearanceStore.LoadAsync();
        themeApplier.Apply(appearance);

        var settingsModule = new SettingsHudModule(
            appPreferencesStore, startupRegistration, appearanceStore, themeApplier, () => appearance);

        // Network access happens only inside the embedded browser, and only when the
        // user imports a workshop (Constitution, Principe V — opt-in).
        ICraftStateStore craftStateStore = new CraftStateStore(logger);
        var craftModule = new CraftHudModule(
            craftStateStore, logger, topmostController, foregroundWatcher, win32Api);

        var modulesBySlotId = new Dictionary<string, IHudModule>
        {
            [accountsModule.ModuleId] = accountsModule,
            [navigationModule.ModuleId] = navigationModule,
            [settingsModule.ModuleId] = settingsModule,
            [craftModule.ModuleId] = craftModule,
        };
        _modulesBySlotId = modulesBySlotId;
        var moduleHost = new ModuleHost(logger);
        _moduleHost = moduleHost;

        var hudWindow = new HudWindow(
            topmostController, foregroundWatcher, layoutStore, layout, modulesBySlotId, moduleHost, win32Api);

        // ShutdownMode is OnExplicitShutdown, so this and the tray menu are the
        // two deliberate ways out; closing a window never ends the session by
        // accident.
        hudWindow.QuitRequested += () =>
        {
            logger.LogInfo("Fermeture demandée depuis la barre du HUD.");
            Shutdown();
        };

        ISwitcherWidgetStore switcherWidgetStore = new SwitcherWidgetStore(logger);
        var switcherPreferences = await switcherWidgetStore.LoadAsync();
        var switcherWidget = new AccountSwitcherWidget(
            accountDetectionService, topmostController, foregroundWatcher, windowActivator, win32Api,
            switcherWidgetStore, switcherPreferences, new WindowIconLoader(win32Api, logger));

        // Declaring a window hideable is just these two lines — the engine is
        // generic, so future companion windows need no new plumbing (FR-012).
        panelVisibility.Register(new HideablePanel("hud", "Barre HUD", hudWindow));
        panelVisibility.Register(new HideablePanel("switcher", "Widget des personnages", switcherWidget));
        PanelContextMenu.Attach(hudWindow, "hud", panelVisibility);
        PanelContextMenu.Attach(switcherWidget, "switcher", panelVisibility);

        _trayIconController = new TrayIconController(panelVisibility);

        // RegisterHotKey needs a real HWND, only available once the window is Loaded.
        hudWindow.Loaded += (_, _) =>
        {
            // One shared listener for both the navigation actions (feature 003)
            // and the per-window un-hide actions (feature 005).
            var hotkeyListener = new GlobalHotkeyListener(hudWindow, win32Api, logger);
            navigationModule.AttachHotkeyListener(hotkeyListener);
            panelVisibility.AttachShortcuts(hotkeyListener);
        };

        // Must precede Show(): the Loaded handler above registers the un-hide
        // shortcuts and can only see what is already loaded.
        await panelVisibility.LoadAsync();

        hudWindow.Show();
        switcherWidget.Show();

        // Re-hides whatever the user had hidden when they last quit (FR-007).
        panelVisibility.ApplyPersistedState();

        await CheckForConflictingSoftwareAsync(
            conflictingSoftwareDetector, processController, appPreferencesStore);
    }

    private static async Task CheckForConflictingSoftwareAsync(
        IConflictingSoftwareDetector detector, IProcessController processController, IAppPreferencesStore preferencesStore)
    {
        var preferences = await preferencesStore.LoadAsync();
        if (preferences.IgnoreConflictWarning)
        {
            return;
        }

        var conflicts = detector.DetectRunning();
        if (conflicts.Count == 0)
        {
            return;
        }

        var warningWindow = new ConflictWarningWindow(conflicts, processController, preferencesStore);
        warningWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIconController?.Dispose();
        if (_moduleHost is not null && _modulesBySlotId is not null)
        {
            foreach (var module in _modulesBySlotId.Values)
            {
                _moduleHost.TryDeactivate(module);
            }
        }

        _foregroundWatcher?.Dispose();
        _logger?.LogInfo("Pofus s'est arrêté normalement.");

        base.OnExit(e);
    }
}
