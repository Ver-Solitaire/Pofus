using System.Windows;

namespace Pofus.Hud;

/// <summary>
/// Interface every future Pofus module (accounts, macros, etc.) must implement to
/// be hosted in a HUD slot. See specs/001-hud-modulaire-dofus/contracts/module-contract.md.
/// This feature ships the contract and its hosting guarantees only — no concrete
/// implementation.
/// </summary>
public interface IHudModule
{
    string ModuleId { get; }

    string DisplayName { get; }

    Uri IconResource { get; }

    bool IsAvailable { get; }

    UIElement CreateContent();

    void OnActivated();

    void OnDeactivated();
}
