using System.Windows;
using System.Windows.Controls;

namespace Pofus.Hud.Panels;

/// <summary>
/// Attaches the "Masquer" right-click menu to a hideable window (FR-001).
/// Generic on purpose: a new window becomes hideable by registering with
/// <see cref="PanelVisibilityService"/> and calling this — no per-window menu
/// code (FR-012).
/// </summary>
public static class PanelContextMenu
{
    public static void Attach(Window window, string panelId, PanelVisibilityService visibilityService)
    {
        var hideItem = new MenuItem { Header = "Masquer cette fenêtre" };
        hideItem.Click += (_, _) => visibilityService.Hide(panelId);

        window.ContextMenu = new ContextMenu { Items = { hideItem } };
    }
}
