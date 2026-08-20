using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Pofus.Core.Accounts;

namespace Pofus.Hud.Switcher;

/// <summary>
/// One entry in the account switcher bar: the character's real taskbar icon
/// (for Dofus, its class icon — see <see cref="WindowIconLoader"/>), falling
/// back to a drawn glyph (<see cref="ClassIconFactory"/>) or a text
/// abbreviation over a class-derived color when a window exposes no icon.
/// A white ring marks whichever chip currently has focus, and orbiting gold
/// dots mark the leader.
/// </summary>
public partial class AccountChipView : UserControl
{
    private nint _windowHandle;
    private bool? _isLeader;

    public event Action<nint>? Activated;

    public AccountChipView()
    {
        InitializeComponent();
    }

    public void Bind(
        string pseudo, string className, nint windowHandle, bool isLeader, BitmapSource? windowIcon)
    {
        _windowHandle = windowHandle;
        ToolTip = isLeader ? $"{pseudo} ({className}) — leader" : $"{pseudo} ({className})";

        if (windowIcon is not null)
        {
            // The real taskbar icon — for Dofus, the character's class icon.
            // It carries its own artwork, so no colored backdrop behind it.
            ClassBadge.Background = Brushes.Transparent;
            ClassBadge.Child = new Image
            {
                Source = windowIcon,
                Stretch = Stretch.Uniform,
                SnapsToDevicePixels = true,
            };
            RenderOptions.SetBitmapScalingMode(ClassBadge, BitmapScalingMode.HighQuality);
        }
        else
        {
            // The window exposed no icon: fall back to the drawn glyph, then to
            // a text abbreviation, over the class-derived color.
            var rgb = ClassColorPalette.ForClassName(className);
            ClassBadge.Background = new SolidColorBrush(Color.FromRgb(rgb.R, rgb.G, rgb.B));
            ClassBadge.Child = ClassIconFactory.TryCreate(className) ?? new TextBlock
            {
                Text = Abbreviate(className),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
            };
        }

        SetLeaderState(isLeader);
    }

    public void SetFocused(bool isFocused) =>
        Ring.BorderBrush = isFocused ? Brushes.White : Brushes.Transparent;

    public void SetLeaderState(bool isLeader)
    {
        // The switcher re-binds every chip every two seconds. Restarting the
        // animations on each pass reset the breathing halo to its first frame,
        // so it never actually breathed — only re-act on a real change.
        if (_isLeader == isLeader)
        {
            return;
        }

        _isLeader = isLeader;

        if (!isLeader)
        {
            LeaderHaloScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            LeaderHaloScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            LeaderHalo.BeginAnimation(OpacityProperty, null);
            LeaderHaloSheen.BeginAnimation(RotateTransform.AngleProperty, null);
            LeaderHalo.Visibility = Visibility.Collapsed;
            return;
        }

        LeaderHalo.Visibility = Visibility.Visible;

        // Breathing: scale and opacity swell together, eased so it feels like a
        // slow pulse rather than a mechanical loop.
        var breathe = new DoubleAnimation(1.0, 1.09, TimeSpan.FromSeconds(1.6))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        };
        LeaderHaloScale.BeginAnimation(ScaleTransform.ScaleXProperty, breathe);
        LeaderHaloScale.BeginAnimation(ScaleTransform.ScaleYProperty, breathe);

        var fade = new DoubleAnimation(0.55, 1.0, TimeSpan.FromSeconds(1.6))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        };
        LeaderHalo.BeginAnimation(OpacityProperty, fade);

        // Iridescent sheen sweeping around the ring, on its own slower cycle so
        // it never lines up with the breathing.
        var sheen = new DoubleAnimation(0, 360, TimeSpan.FromSeconds(3.2))
        {
            RepeatBehavior = RepeatBehavior.Forever,
        };
        LeaderHaloSheen.BeginAnimation(RotateTransform.AngleProperty, sheen);
    }

    private static string Abbreviate(string className)
    {
        var letters = className.Where(char.IsLetter).Take(2).ToArray();
        return letters.Length > 0 ? new string(letters).ToUpperInvariant() : "?";
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Marked handled so the click stops here. Left to bubble, it also reached
        // the widget's drag handler, which then called DragMove() on a window
        // that had just given the foreground away to the game — the exact
        // sequence that crashed Pofus. Dragging stays on the grip.
        e.Handled = true;
        Activated?.Invoke(_windowHandle);
    }
}
