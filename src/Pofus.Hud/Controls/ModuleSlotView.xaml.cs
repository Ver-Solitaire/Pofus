using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Pofus.Core.Models;

namespace Pofus.Hud.Controls;

/// <summary>
/// A single HUD emplacement. Renders visibly differently when
/// <see cref="State"/> is Empty (FR-007 — never a broken-looking blank slot).
/// </summary>
public partial class ModuleSlotView : UserControl
{
    public static readonly DependencyProperty StateProperty = DependencyProperty.Register(
        nameof(State), typeof(ModuleSlotState), typeof(ModuleSlotView),
        new PropertyMetadata(ModuleSlotState.Empty, OnStateChanged));

    public static readonly DependencyProperty SlotLabelProperty = DependencyProperty.Register(
        nameof(SlotLabel), typeof(string), typeof(ModuleSlotView),
        new PropertyMetadata(string.Empty, OnSlotLabelChanged));

    public ModuleSlotView()
    {
        InitializeComponent();
        ApplyState(ModuleSlotState.Empty);
    }

    public ModuleSlotState State
    {
        get => (ModuleSlotState)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public string SlotLabel
    {
        get => (string)GetValue(SlotLabelProperty);
        set => SetValue(SlotLabelProperty, value);
    }

    private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((ModuleSlotView)d).ApplyState((ModuleSlotState)e.NewValue);

    private static void OnSlotLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (ModuleSlotView)d;
        var label = (string)e.NewValue;
        view.SlotLabelText.Text = label;
        view.ToolTip = label;
    }

    private void ApplyState(ModuleSlotState state)
    {
        var isEmpty = state == ModuleSlotState.Empty;
        SlotBorder.Opacity = isEmpty ? 0.4 : 1.0;
        SlotBorder.BorderBrush = (Brush)FindResource(
            isEmpty ? "Pofus.BorderSubtle" : "Pofus.Accent");
    }

    /// <summary>Replaces the placeholder label with a hosted module's real
    /// content (see <c>ModuleHost</c>), keeping this control's border chrome.</summary>
    public void SetModuleContent(UIElement content)
    {
        SlotBorder.Child = content;
    }
}
