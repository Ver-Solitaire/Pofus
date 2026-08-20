using System.Runtime.InteropServices;
using System.Security;
using Pofus.Core.Logging;

namespace Pofus.Platform;

/// <summary>
/// Makes Pofus start with Windows by placing a shortcut in the per-user
/// Startup folder (the one <c>shell:startup</c> opens).
///
/// A Run registry value would work too, but the Startup folder is where users
/// actually look — it is inspectable and removable without a registry editor,
/// which matters for a portable executable the user may move or delete: a
/// stale shortcut is obvious and trivial to clean up, a stale registry value
/// is neither. No admin rights are involved either way.
///
/// The shortcut name is configurable so tests target a throwaway file instead
/// of the real "Pofus" entry.
/// </summary>
public sealed class StartupRegistration : IStartupRegistration
{
    private readonly IAppLogger _logger;
    private readonly string _shortcutPath;
    private readonly string _executablePath;

    public StartupRegistration(IAppLogger logger, string? shortcutName = null, string? executablePath = null)
    {
        _logger = logger;
        _executablePath = executablePath ?? Environment.ProcessPath ?? string.Empty;
        _shortcutPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            $"{shortcutName ?? "Pofus"}.lnk");
    }

    public bool IsEnabled()
    {
        try
        {
            return File.Exists(_shortcutPath);
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        {
            _logger.LogWarning($"Failed to read startup shortcut state: {ex.Message}");
            return false;
        }
    }

    public bool TryEnable(out string? error)
    {
        if (string.IsNullOrEmpty(_executablePath))
        {
            error = "Chemin de l'exécutable introuvable.";
            _logger.LogWarning("Cannot enable startup: executable path unavailable.");
            return false;
        }

        try
        {
            var directory = Path.GetDirectoryName(_shortcutPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            ShellLink.Create(_shortcutPath, _executablePath, "Pofus — assistant multi-comptes Dofus");
            error = null;
            return true;
        }
        catch (Exception ex) when (ex is COMException or SecurityException or UnauthorizedAccessException or IOException)
        {
            error = ex.Message;
            _logger.LogWarning($"Failed to create the startup shortcut: {ex.Message}");
            return false;
        }
    }

    public bool TryDisable(out string? error)
    {
        try
        {
            // Deleting a file that is not there is the desired end state, not
            // a failure, so no existence check is needed.
            File.Delete(_shortcutPath);
            error = null;
            return true;
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        {
            error = ex.Message;
            _logger.LogWarning($"Failed to remove the startup shortcut: {ex.Message}");
            return false;
        }
    }
}
