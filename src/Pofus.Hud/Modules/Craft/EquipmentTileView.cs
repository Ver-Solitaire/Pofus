using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Pofus.Core.Craft;

namespace Pofus.Hud.Modules.Craft;

/// <summary>
/// One equipment shown as a large tile: artwork, name, and a quantity badge —
/// the "big picture" view of the workshop, meant to be read at a glance rather
/// than scanned line by line.
///
/// Not a checklist: what gets ticked off is the shopping. Two independent tick
/// systems in the same window would only raise the question of which one counts.
/// </summary>
public sealed class EquipmentTileView : Border
{
    private const double TileSize = 132;

    public EquipmentTileView(CraftItem item, ItemImageLoader imageLoader)
    {
        Width = TileSize;
        Margin = new Thickness(0, 0, 8, 8);
        Padding = new Thickness(8);
        CornerRadius = new CornerRadius(6);
        Background = (Brush)Application.Current.Resources["Pofus.SurfaceRaised"];
        ToolTip = $"{item.Name} ×{item.Quantity}";

        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var artwork = new Image
        {
            Source = imageLoader.TryGet(item.Picture, large: true),
            Stretch = Stretch.Uniform,
            Height = 70,
            Margin = new Thickness(0, 2, 0, 6),
        };
        RenderOptions.SetBitmapScalingMode(artwork, BitmapScalingMode.HighQuality);
        Grid.SetRow(artwork, 0);
        layout.Children.Add(artwork);

        var name = new TextBlock
        {
            Text = item.Name,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            FontSize = 11,
            MaxHeight = 46, // two lines; longer names are trimmed rather than stretching the tile
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        Grid.SetRow(name, 1);
        layout.Children.Add(name);

        // Quantity sits over the artwork so every tile stays the same height
        // whatever the length of its name.
        var badge = new Border
        {
            Background = (Brush)Application.Current.Resources["Pofus.Accent"],
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(5, 1, 5, 1),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Visibility = item.Quantity > 1 ? Visibility.Visible : Visibility.Collapsed,
            Child = new TextBlock
            {
                Text = $"×{item.Quantity}",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
            },
        };

        var root = new Grid();
        root.Children.Add(layout);
        root.Children.Add(badge);
        Child = root;
    }
}
