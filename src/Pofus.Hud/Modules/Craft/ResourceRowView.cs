using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Pofus.Core.Craft;

namespace Pofus.Hud.Modules.Craft;

/// <summary>
/// One shopping-list line: a tick box, a clickable name that copies itself to
/// the clipboard, and the total quantity. Built in code rather than XAML
/// because rows are generated per import; it mirrors the checkbox-plus-store
/// pattern already used by the account manager (feature 002) rather than
/// introducing a second checklist mechanism.
/// </summary>
public sealed class ResourceRowView : Grid
{
    private static readonly TimeSpan CopiedFeedbackDuration = TimeSpan.FromSeconds(1.5);

    private readonly TextBlock _nameText;
    private readonly TextBlock _copiedText;
    private readonly CheckBox _checkBox;
    private readonly DispatcherTimer _feedbackTimer;
    private readonly string _resourceName;

    public event Action<int, bool>? GatheredChanged;

    public ResourceRowView(RequiredResource resource, bool isGathered, ItemImageLoader imageLoader)
    {
        _resourceName = resource.Name;
        Margin = new Thickness(0, 3, 0, 3);

        ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // tick
        ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // icon
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _checkBox = new CheckBox
        {
            IsChecked = isGathered,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            ToolTip = "Marquer comme récupérée",
        };
        _checkBox.Checked += (_, _) => RaiseGathered(resource.ItemId, true);
        _checkBox.Unchecked += (_, _) => RaiseGathered(resource.ItemId, false);
        SetColumn(_checkBox, 0);
        Children.Add(_checkBox);

        // Fixed-size holder so rows stay aligned whether or not the icon loads.
        var iconHolder = new Border
        {
            Width = 24,
            Height = 24,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new Image
            {
                Source = imageLoader.TryGet(resource.Picture),
                Stretch = Stretch.Uniform,
            },
        };
        SetColumn(iconHolder, 1);
        Children.Add(iconHolder);

        _nameText = new TextBlock
        {
            Text = resource.Name,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Cursor = Cursors.Hand,
            ToolTip = $"Cliquer pour copier « {resource.Name} »",
        };
        _nameText.MouseLeftButtonUp += (_, _) => CopyName();
        SetColumn(_nameText, 2);
        Children.Add(_nameText);

        _copiedText = new TextBlock
        {
            Text = "✓ Copié !",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 6, 0),
            Visibility = Visibility.Collapsed,
            Foreground = (Brush)Application.Current.Resources["Pofus.Accent"],
        };
        SetColumn(_copiedText, 3);
        Children.Add(_copiedText);

        var quantity = new TextBlock
        {
            Text = $"×{resource.TotalQuantity}",
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 46,
            TextAlignment = TextAlignment.Right,
            FontWeight = FontWeights.SemiBold,
        };
        SetColumn(quantity, 4);
        Children.Add(quantity);

        _feedbackTimer = new DispatcherTimer { Interval = CopiedFeedbackDuration };
        _feedbackTimer.Tick += (_, _) =>
        {
            _feedbackTimer.Stop();
            _copiedText.Visibility = Visibility.Collapsed;
        };

        ApplyGatheredLook(isGathered);
    }

    private void RaiseGathered(int itemId, bool isGathered)
    {
        ApplyGatheredLook(isGathered);
        GatheredChanged?.Invoke(itemId, isGathered);
    }

    /// <summary>A gathered resource is struck through and dimmed, so what is
    /// still missing stands out at a glance.</summary>
    private void ApplyGatheredLook(bool isGathered)
    {
        _nameText.TextDecorations = isGathered ? TextDecorations.Strikethrough : null;
        Opacity = isGathered ? 0.5 : 1.0;
    }

    /// <summary>
    /// Copies the resource name only — never the quantity — so it can be pasted
    /// straight into the in-game market search.
    /// </summary>
    private void CopyName()
    {
        try
        {
            Clipboard.SetText(_resourceName);
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // The clipboard is briefly lockable by other processes; a failed
            // copy must not take the window down. Skip the confirmation so the
            // user is not told something happened when it did not.
            return;
        }

        _copiedText.Visibility = Visibility.Visible;
        _feedbackTimer.Stop();
        _feedbackTimer.Start();
    }
}
