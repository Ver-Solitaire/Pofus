using System.Drawing;
using System.Windows.Forms;
using Pofus.Hud.Panels;
using Application = System.Windows.Application;

namespace Pofus.App;

/// <summary>
/// System tray affordance. Since feature 005 it is the guaranteed way back:
/// it lists every hideable window so the user can bring one back even with no
/// shortcut bound and every window hidden (FR-009). Uses
/// SystemIcons.Application as a placeholder — a branded icon asset is still
/// pending (see specs/001-hud-modulaire-dofus/tasks.md T027).
/// </summary>
internal sealed class TrayIconController : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly PanelVisibilityService _visibilityService;

    public TrayIconController(PanelVisibilityService visibilityService)
    {
        _visibilityService = visibilityService;

        var contextMenu = new ContextMenuStrip();
        // Rebuilt on each open so the checkmarks reflect the current state.
        contextMenu.Opening += (_, _) => RebuildMenu(contextMenu);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "Pofus",
            ContextMenuStrip = contextMenu,
        };
    }

    private void RebuildMenu(ContextMenuStrip menu)
    {
        menu.Items.Clear();
        foreach (var panel in _visibilityService.GetPanels())
        {
            var panelId = panel.PanelId;
            var item = new ToolStripMenuItem(panel.DisplayName)
            {
                Checked = !_visibilityService.IsHidden(panelId),
                CheckOnClick = false,
            };
            item.Click += (_, _) =>
            {
                if (_visibilityService.IsHidden(panelId))
                {
                    _visibilityService.Show(panelId);
                }
                else
                {
                    _visibilityService.Hide(panelId);
                }
            };
            menu.Items.Add(item);
        }

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quitter", null, (_, _) => Application.Current.Shutdown());
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
