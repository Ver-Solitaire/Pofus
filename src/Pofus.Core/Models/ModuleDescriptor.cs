namespace Pofus.Core.Models;

/// <summary>
/// Minimal identity of a module referenced by a <see cref="ModuleSlot"/>. The
/// module's actual content/behavior is out of scope for the HUD feature (see
/// spec.md Assumptions) — this only carries what the HUD needs to render a slot.
/// </summary>
public sealed class ModuleDescriptor
{
    public required string ModuleId { get; init; }

    public required string DisplayName { get; init; }

    public required Uri IconResource { get; init; }
}
