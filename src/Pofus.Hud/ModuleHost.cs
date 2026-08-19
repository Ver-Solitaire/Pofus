using System.Windows;
using Pofus.Core.Logging;

namespace Pofus.Hud;

/// <summary>
/// Enforces the hosting guarantees promised in
/// specs/001-hud-modulaire-dofus/contracts/module-contract.md: any exception a
/// module raises from CreateContent/OnActivated/OnDeactivated is caught, logged
/// explicitly (Principle I), and degrades that slot to empty rather than
/// crashing the HUD.
/// </summary>
public sealed class ModuleHost
{
    private readonly IAppLogger _logger;

    public ModuleHost(IAppLogger logger)
    {
        _logger = logger;
    }

    /// <summary>Returns the module's content, or null if hosting it failed.</summary>
    public UIElement? TryCreateContent(IHudModule module)
    {
        if (!module.IsAvailable)
        {
            return null;
        }

        try
        {
            return module.CreateContent();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Module '{module.ModuleId}' failed in CreateContent(); hosting it as empty.", ex);
            return null;
        }
    }

    public void TryActivate(IHudModule module)
    {
        try
        {
            module.OnActivated();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Module '{module.ModuleId}' threw from OnActivated().", ex);
        }
    }

    public void TryDeactivate(IHudModule module)
    {
        try
        {
            module.OnDeactivated();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Module '{module.ModuleId}' threw from OnDeactivated().", ex);
        }
    }
}
