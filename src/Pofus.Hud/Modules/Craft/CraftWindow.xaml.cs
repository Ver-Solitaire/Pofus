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

    private const double BigPictureWidth = 452;
    private const double CollapsedMinWidth = 380;

    private readonly ICraftStateStore _stateStore;
    private readonly IAppLogger _logger;
    private readonly WorkshopBrowserImporter _browserImporter;
    private readonly ItemImageLoader _imageLoader;
    private CraftState _state = new();
    private bool _isImporting;
    private bool _isBigPictureOpen;
    private double _collapsedWidth = 470;

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
        if (!string.IsNullOrWhiteSpace(_state.WorkshopUrl))
        {
            UrlBox.Text = _state.WorkshopUrl;
        }

        RenderResources();
    }

    /// <summary>Re-reads the workshop that produced the current list.</summary>
    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        if (_isImporting || string.IsNullOrWhiteSpace(_state.WorkshopUrl))
        {
            return;
        }

        UrlBox.Text = _state.WorkshopUrl;
        await ImportFromUrlAsync(_state.WorkshopUrl);
    }

    // Bound to Checked/Unchecked rather than Click: the state can also change
    // from the keyboard or from automation, where Click never fires.
    private void OnToggleEquipmentChanged(object sender, RoutedEventArgs e) => RenderEquipment();

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

        await ImportFromUrlAsync(UrlBox.Text);
    }

    private async Task ImportFromUrlAsync(string? rawUrl)
    {
        if (!WorkshopBrowserImporter.IsSupportedWorkshopUrl(rawUrl, out var url))
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
            await DisplayAsync(crafts, url!.ToString());
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
    private async Task DisplayAsync(IReadOnlyList<WorkshopCraft> crafts, string workshopUrl)
    {
        var resources = WorkshopParser.BuildShoppingList(crafts);
        if (resources.Count == 0)
        {
            SetStatus("Cet atelier ne contient aucune ressource.", isError: true);
            return;
        }

        // Ticks survive a refresh: they are keyed on the resource's item id, so
        // only resources that genuinely left the workshop lose their state.
        var stillPresent = resources.Select(r => r.ItemId).ToHashSet();
        _state.GatheredItemIds.RemoveWhere(id => !stillPresent.Contains(id));
        _state.WorkshopUrl = workshopUrl;
        _state.Equipment = crafts
            .Select(c => new CraftItem(c.ItemId, c.Name, c.Quantity, c.Picture))
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

        // Refreshing needs a workshop to re-read; without one the button would
        // only be able to fail.
        RefreshButton.IsEnabled = !string.IsNullOrWhiteSpace(_state.WorkshopUrl);
        ShowEquipmentToggle.IsEnabled = _state.Equipment.Count > 0;

        foreach (var resource in _state.Resources)
        {
            var row = new ResourceRowView(resource, _state.GatheredItemIds.Contains(resource.ItemId), _imageLoader);
            row.GatheredChanged += OnGatheredChanged;
            ResourceRowsPanel.Children.Add(row);
        }

        RenderEquipment();
        UpdateProgress();
    }

    /// <summary>
    /// Shows or hides the tile panel, widening the window to make room instead
    /// of squeezing the shopping list — that list is the part one reads while
    /// buying, and it should not shrink to display context.
    /// </summary>
    private void RenderEquipment()
    {
        var show = ShowEquipmentToggle.IsChecked == true && _state.Equipment.Count > 0;
        if (show == _isBigPictureOpen)
        {
            return;
        }

        _isBigPictureOpen = show;
        BigPicturePanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        EquipmentTilesPanel.Children.Clear();

        if (show)
        {
            foreach (var item in _state.Equipment.OrderBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                EquipmentTilesPanel.Children.Add(new EquipmentTileView(item, _imageLoader));
            }

            // Remember how wide the list was so collapsing restores exactly the
            // size the user had chosen, rather than a hardcoded default.
            _collapsedWidth = Width;
            MinWidth = CollapsedMinWidth + BigPictureWidth;
            Width = _collapsedWidth + BigPictureWidth;
        }
        else
        {
            // Lower the floor BEFORE shrinking: WPF clamps Width to MinWidth, so
            // doing it the other way round leaves the window stuck wide open.
            MinWidth = CollapsedMinWidth;
            Width = _collapsedWidth;
        }
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
        RefreshButton.IsEnabled = !isBusy && !string.IsNullOrWhiteSpace(_state.WorkshopUrl);
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
