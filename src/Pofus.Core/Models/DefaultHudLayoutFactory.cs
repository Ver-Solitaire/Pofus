namespace Pofus.Core.Models;

/// <summary>
/// The fixed slot arrangement used when no persisted layout exists yet (first
/// launch). Slot order/count is fixed for this version — no drag-and-drop
/// reordering (FR-008). Slot identities mirror the module families the HUD is
/// meant to host later (accounts, macros, group actions, radial menu,
/// settings) — none are implemented by this feature (spec.md Assumptions).
/// </summary>
public static class DefaultHudLayoutFactory
{
    private static readonly string[] DefaultSlotIds =
    [
        "accounts",
        "macros",
        "group-actions",
        "radial-menu",
        "settings",
    ];

    public static HudLayout CreateDefault()
    {
        var slots = DefaultSlotIds
            .Select((slotId, index) => new ModuleSlot { SlotId = slotId, Position = index })
            .ToList();

        return new HudLayout { Slots = slots };
    }
}
