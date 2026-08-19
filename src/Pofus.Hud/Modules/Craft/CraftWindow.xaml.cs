using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Pofus.Core.Craft;
using Pofus.Core.Logging;
using Pofus.Platform;

namespace Pofus.Hud.Modules.Craft;

/// <summary>
/// Shopping list for a DofusBook workshop: import, aggregate, tick off.
/// The tick state is persisted the same way account preferences are, so there
/// is a single checklist mechanism in Pofus, not two.
/// </summary>
public partial class CraftWindow : Window
{

    private readonly ICraftStateStore _stateStore;
    private readonly IAppLogger _logger;
    private readonly WorkshopBrowserImporter _browserImporter;
    private readonly ItemImageLoader _imageLoader;
    private CraftState _state = new();
    private bool _isImporting;

    public CraftWindow(
        ICraftStateStore stateStore,
        IAppLogger logger,
        ITopmostWindowController topmostController,
        ForegroundWindowWatcher foregroundWatcher,
        IWin32WindowApi win32Api)
    {
        InitializeComponent();
        _stateStore = stateStore;
        _logger = logger;
        _browserImporter = new WorkshopBrowserImporter(logger);
        _imageLoader = new ItemImageLoader(logger);
        WindowResizer.Attach(this);
        TopmostKeeper.Attach(this, topmostController, foregroundWatcher, win32Api);

        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _state = await _stateStore.LoadAsync();
        RenderResources();
    }

    private void OnHeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnUrlKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            OnImportClick(sender, e);
        }
    }

    /// <summary>Imports straight from a pasted workshop link, via the embedded browser.</summary>
    private async void OnImportClick(object sender, RoutedEventArgs e)
    {
        if (_isImporting)
        {
            return;
        }

        if (!WorkshopBrowserImporter.IsSupportedWorkshopUrl(UrlBox.Text, out var url))
        {
            SetStatus(
                "Lien invalide. Attendu : un lien DofusBook, par exemple https://d-bk.net/fr/dw/XXXX",
                isError: true);
            return;
        }

        SetBusy(true, "Lecture de l'atelier…");
        try
        {
            var crafts = await _browserImporter.ImportAsync(url!);
            await DisplayAsync(crafts);
        }
        catch (CraftDataUnavailableException ex)
        {
            SetStatus(ex.Message, isError: true);
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    /// <summary>
    /// Builds the shopping list and shows it. DofusBook's workshop already
    /// carries every ingredient's label and per-unit count, so this needs no
    /// external lookup — the totals are computed straight from the import.
    /// </summary>
    private async Task DisplayAsync(IReadOnlyList<WorkshopCraft> crafts)
    {
        var resources = WorkshopParser.BuildShoppingList(crafts);
        if (resources.Count == 0)
        {
            SetStatus("Cet atelier ne contient aucune ressource.", isError: true);
            return;
        }

        // Keep ticks for resources still needed, drop those that left the list.
        var stillPresent = resources.Select(r => r.ItemId).ToHashSet();
        _state.GatheredItemIds.RemoveWhere(id => !stillPresent.Contains(id));
        _state.Equipment = crafts
            .Select(c => new CraftItem(c.ItemId, c.Name, c.Quantity))
            .ToList();
        _state.Resources = resources.ToList();
        await _stateStore.SaveAsync(_state);

        RenderResources();
        SetStatus($"{crafts.Count} équipement(s), {resources.Count} ressource(s).", isError: false);
    }

    private void RenderResources()
    {
        ResourceRowsPanel.Children.Clear();

        var hasResources = _state.Resources.Count > 0;
        EmptyStateText.Visibility = hasResources ? Visibility.Collapsed : Visibility.Visible;
        SummaryText.Text = hasResources
            ? $"{_state.Equipment.Count} équipement(s) · {_state.Resources.Count} ressources différentes"
            : string.Empty;

        foreach (var resource in _state.Resources)
        {
            var row = new ResourceRowView(resource, _state.GatheredItemIds.Contains(resource.ItemId), _imageLoader);
            row.GatheredChanged += OnGatheredChanged;
            ResourceRowsPanel.Children.Add(row);
        }

        UpdateProgress();
    }

    private async void OnGatheredChanged(int itemId, bool isGathered)
    {
        if (isGathered)
        {
            _state.GatheredItemIds.Add(itemId);
        }
        else
        {
            _state.GatheredItemIds.Remove(itemId);
        }

        UpdateProgress();
        await _stateStore.SaveAsync(_state);
    }

    private void UpdateProgress()
    {
        if (_state.Resources.Count == 0)
        {
            ProgressText.Text = string.Empty;
            return;
        }

        var gathered = _state.Resources.Count(r => _state.GatheredItemIds.Contains(r.ItemId));
        ProgressText.Text = $"{gathered} / {_state.Resources.Count} ressources récupérées";
    }

    private void SetBusy(bool isBusy, string? message)
    {
        _isImporting = isBusy;
        ImportButton.IsEnabled = !isBusy;
        Cursor = isBusy ? Cursors.Wait : null;
        if (message is not null)
        {
            SetStatus(message, isError: false);
        }
    }

    private void SetStatus(string message, bool isError)
    {
        StatusText.Foreground = (Brush)FindResource(isError ? "Pofus.Danger" : "Pofus.Accent");
        StatusText.Text = message;
    }
}
