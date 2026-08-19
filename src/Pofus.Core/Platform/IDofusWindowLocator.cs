using Pofus.Core.Models;

namespace Pofus.Core.Platform;

/// <summary>
/// Abstraction over Dofus window detection. Lives in Pofus.Core (not
/// Pofus.Platform, where the Win32-backed implementation lives) so Core-level
/// consumers — e.g. the accounts module — can depend on it without taking a
/// dependency on Pofus.Platform (Principe III: Platform depends on Core, never
/// the reverse).
/// </summary>
public interface IDofusWindowLocator
{
    /// <summary>
    /// Enumerates every currently open, visible Dofus game window. Never throws —
    /// enumeration failures are logged and result in an empty list (cf. spec.md
    /// Edge Cases: "aucune fenêtre Dofus détectée").
    /// </summary>
    IReadOnlyList<DofusWindowInfo> GetOpenDofusWindows();
}
