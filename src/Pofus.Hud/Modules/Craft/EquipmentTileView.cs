using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Pofus.Core.Craft;

namespace Pofus.Hud.Modules.Craft;

/// <summary>
/// One equipment shown as a large tile: artwork, name, and a quantity badge —
/// the "big picture" view of the workshop, meant to be read at a glance rather
/// than scanned line by line.
///
/// Clicking a tile marks the equipment as crafted: the outline turns green and
/// the tile glows. That is a different milestone from the shopping list's ticks,
/// which track resources bought, so the two states are stored separately.
/// </summary>
public sealed class EquipmentTileView : Border
{
    private const double TileSize = 132;

    /// <summary>How far the crafted glow spreads. Wide enough to read across the
    /// panel, tight enough not to bleed into the neighbouring tile.</summary>
    private const double GlowBlur = 18;

    private readonly int _itemId;
    private readonly DropShadowEffect _glow;
    private bool _isCrafted;

    /// <summary>Raised with the equipment id and its new crafted state.</summary>
    public event Action<int, bool>? CraftedChanged;

    public EquipmentTileView(CraftItem item, ItemImageLoader imageLoader, bool isCrafted)
    {
        _itemId = item.ItemId;
        _isCrafted = isCrafted;

        Width = TileSize;
        Margin = new Thickness(0, 0, 8, 8);
        Padding = new Thickness(8);
        CornerRadius = new CornerRadius(6);
        Cursor = Cursors.Hand;

        // Filled with the same black the icons are composited over, so the
        // icon's own square dissolves into the tile rather than sitting inside
        // it as a second box.
        Background = (Brush)Application.Current.Resources["Pofus.TileFill"];
        BorderThickness = new Thickness(1);

        _glow = new DropShadowEffect
        {
            Color = ((SolidColorBrush)Application.Current.Resources["Pofus.Success"]).Color,
            BlurRadius = GlowBlur,
            ShadowDepth = 0,
            Opacity = 0,
        };
        // Attached only while it has something to show: a workshop can hold
        // dozens of tiles, and leaving a fully transparent effect on every one
        // of them costs a render pass each for nothing (Principe II).

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

        MouseLeftButtonUp += OnMouseLeftButtonUp;
        MouseEnter += (_, _) => ApplyCraftedLook(animate: false);
        MouseLeave += (_, _) => ApplyCraftedLook(animate: false);

        ApplyCraftedLook(animate: false);
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _isCrafted = !_isCrafted;
        ApplyCraftedLook(animate: true);
        CraftedChanged?.Invoke(_itemId, _isCrafted);
    }

    /// <summary>
    /// Crafted: a green outline plus a green glow around the tile, so a finished
    /// item is obvious across the whole panel rather than only on close reading.
    /// Otherwise a white hairline, brighter under the pointer.
    /// </summary>
    private void ApplyCraftedLook(bool animate)
    {
        if (_isCrafted)
        {
            BorderBrush = (Brush)Application.Current.Resources["Pofus.Success"];
            BorderThickness = new Thickness(1.6);
            ToolTip = "Fabriqué — cliquer pour annuler";
            SetGlow(IsMouseOver ? 1.0 : 0.85, animate);
            return;
        }

        BorderBrush = (Brush)Application.Current.Resources[
            IsMouseOver ? "Pofus.OutlineHover" : "Pofus.Outline"];
        BorderThickness = new Thickness(1);
        ToolTip = "Cliquer une fois fabriqué";
        SetGlow(0, animate);
    }

    /// <summary>
    /// The glow fades in rather than snapping on, so ticking a tile reads as a
    /// confirmation instead of a flicker. Animated only on a real click: a
    /// hover, or the initial build of forty tiles, must not start forty
    /// storyboards.
    /// </summary>
    private void SetGlow(double opacity, bool animate)
    {
        if (!animate)
        {
            _glow.BeginAnimation(DropShadowEffect.OpacityProperty, null);
            _glow.Opacity = opacity;
            Effect = opacity > 0 ? _glow : null;
            return;
        }

        // Attached before fading in, detached only once it has faded out.
        Effect = _glow;

        var fade = new DoubleAnimation(opacity, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut },
        };

        if (opacity == 0)
        {
            fade.Completed += (_, _) =>
            {
                if (!_isCrafted)
                {
                    Effect = null;
                }
            };
        }

        _glow.BeginAnimation(DropShadowEffect.OpacityProperty, fade);
    }
}
