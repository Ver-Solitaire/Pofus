namespace Pofus.Core.Models;

public enum ModuleSlotState
{
    Empty,
    Occupied,
}

/// <summary>
/// An emplacement in the HUD's fixed layout. Position is set by the layout
/// definition, not by the user (FR-008 — no drag-and-drop reordering in v1).
/// </summary>
public sealed class ModuleSlot
{
    public required string SlotId { get; init; }

    public required int Position { get; init; }

    public ModuleSlotState State { get; set; } = ModuleSlotState.Empty;

    public string? ModuleId { get; set; }
}
