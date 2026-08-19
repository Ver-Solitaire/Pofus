namespace Pofus.Core.Models;

public sealed class HudPosition
{
    public double X { get; set; }

    public double Y { get; set; }
}

/// <summary>
/// Persistent state of the HUD across sessions (FR-006, SC-005): its screen
/// position and the fixed, ordered set of module slots.
/// </summary>
public sealed class HudLayout
{
    public HudPosition WindowPosition { get; set; } = new();

    public bool IsVisible { get; set; } = true;

    public List<ModuleSlot> Slots { get; set; } = [];
}
