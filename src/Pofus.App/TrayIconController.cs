using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Pofus.Hud.Panels;
using Application = System.Windows.Application;

namespace Pofus.App;

/// <summary>
/// System tray affordance. Since feature 005 it is the guaranteed way back:
/// it lists every hideable window so the user can bring one back even with no
/// shortcut bound and every window hidden (FR-009).
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
            Icon = LoadAppIcon(),
            Visible = true,
            Text = "Pofus",
            ContextMenuStrip = contextMenu,
        };
    }

    /// <summary>
    /// The application icon, at the exact size the notification area asks for.
    ///
    /// Requesting <see cref="SystemInformation.SmallIconSize"/> makes Windows
    /// pick the matching frame from the .ico; the parameterless constructor
    /// takes whichever frame comes first and leaves the shell to shrink it,
    /// which turns the artwork to mush at 16 px and follows the display scaling
    /// badly.
    ///
    /// Falls back to the generic Windows icon rather than leaving the tray
    /// empty — an invisible tray icon would strand the user with no way to
    /// bring hidden windows back.
    /// </summary>
    private static Icon LoadAppIcon()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "pofus.ico");
            if (File.Exists(path))
            {
                var size = SystemInformation.SmallIconSize;
                return new Icon(path, size.Width, size.Height);
            }
        }
        catch (Exception ex) when (ex is IOException or ArgumentException)
        {
            // Falls through to the system icon below.
        }

        return SystemIcons.Application;
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
