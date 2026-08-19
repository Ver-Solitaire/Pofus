using System.Security;
using Microsoft.Win32;
using Pofus.Core.Logging;

namespace Pofus.Platform;

/// <summary>
/// Registers Pofus to launch at Windows sign-in via the standard per-user
/// Run key (no admin rights needed) — simpler and less orphan-prone than a
/// Scheduled Task or a Startup-folder shortcut (research.md). The value name
/// is configurable so tests can target a throwaway entry instead of the real
/// "Pofus" value.
/// </summary>
public sealed class StartupRegistration : IStartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private readonly IAppLogger _logger;
    private readonly string _valueName;
    private readonly string _executablePath;

    public StartupRegistration(IAppLogger logger, string? valueName = null, string? executablePath = null)
    {
        _logger = logger;
        _valueName = valueName ?? "Pofus";
        _executablePath = executablePath ?? Environment.ProcessPath ?? string.Empty;
    }

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(_valueName) is not null;
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException)
        {
            _logger.LogWarning($"Failed to read startup registration state: {ex.Message}");
            return false;
        }
    }

    public bool TryEnable(out string? error)
    {
        if (string.IsNullOrEmpty(_executablePath))
        {
            error = "Chemin de l'exécutable introuvable.";
            _logger.LogWarning("Cannot enable startup registration: executable path unavailable.");
            return false;
        }

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            key.SetValue(_valueName, $"\"{_executablePath}\"");
            error = null;
            return true;
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        {
            error = ex.Message;
            _logger.LogWarning($"Failed to enable startup registration: {ex.Message}");
            return false;
        }
    }

    public bool TryDisable(out string? error)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            key?.DeleteValue(_valueName, throwOnMissingValue: false);
            error = null;
            return true;
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        {
            error = ex.Message;
            _logger.LogWarning($"Failed to disable startup registration: {ex.Message}");
            return false;
        }
    }
}
