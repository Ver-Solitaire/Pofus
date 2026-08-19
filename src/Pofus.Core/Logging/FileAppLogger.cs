using System.Diagnostics;

namespace Pofus.Core.Logging;

/// <summary>
/// Writes log entries to %APPDATA%\Pofus\logs\pofus.log. If the write itself fails
/// (disk full, permissions), the entry falls back to Debug.WriteLine rather than
/// being dropped silently — Principle I forbids swallowing errors without a trace.
/// </summary>
public sealed class FileAppLogger : IAppLogger
{
    private readonly string _logFilePath;
    private readonly object _writeLock = new();

    public FileAppLogger(string? logFilePath = null)
    {
        _logFilePath = logFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Pofus", "logs", "pofus.log");
    }

    public void LogInfo(string message) => Write("INFO", message);

    public void LogWarning(string message) => Write("WARN", message);

    public void LogError(string message, Exception? exception = null)
    {
        var full = exception is null ? message : $"{message} :: {exception}";
        Write("ERROR", full);
    }

    private void Write(string level, string message)
    {
        var line = $"{DateTimeOffset.Now:O} [{level}] {message}{Environment.NewLine}";
        lock (_writeLock)
        {
            try
            {
                var directory = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(_logFilePath, line);
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"[Pofus] Failed to write log file: {ex.Message}");
                Debug.WriteLine(line);
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"[Pofus] Log file access denied: {ex.Message}");
                Debug.WriteLine(line);
            }
        }
    }
}
