using Pofus.Core.Navigation;

namespace Pofus.Hud.Modules.Navigation;

/// <summary>
/// Maps the closed <see cref="NavigationAction"/> enum onto the opaque string
/// action ids that <see cref="GlobalHotkeyListener"/> now uses, so navigation
/// shortcuts can share one listener with feature 005's per-module un-hide
/// shortcuts without their ids colliding (data-model.md).
/// </summary>
internal static class NavigationActionIds
{
    private const string Prefix = "nav:";

    public static string ToActionId(NavigationAction action) => Prefix + action;

    public static bool TryParse(string actionId, out NavigationAction action)
    {
        action = default;
        return actionId.StartsWith(Prefix, StringComparison.Ordinal)
            && Enum.TryParse(actionId[Prefix.Length..], out action);
    }
}
