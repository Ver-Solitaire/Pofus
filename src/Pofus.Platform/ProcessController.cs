using System.ComponentModel;
using System.Diagnostics;
using Pofus.Core.Logging;
using Pofus.Core.Settings;

namespace Pofus.Platform;

/// <summary>
/// Real implementation of <see cref="IProcessController"/> via
/// System.Diagnostics.Process — no P/Invoke needed. Kill failures are
/// reported through <paramref name="error"/>, never swallowed (Principe I).
/// </summary>
public sealed class ProcessController : IProcessController
{
    private readonly IAppLogger _logger;

    public ProcessController(IAppLogger logger)
    {
        _logger = logger;
    }

    public bool IsRunning(string processName)
    {
        try
        {
            var processes = Process.GetProcessesByName(processName);
            try
            {
                return processes.Length > 0;
            }
            finally
            {
                DisposeAll(processes);
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning($"Failed to check whether '{processName}' is running: {ex.Message}");
            return false;
        }
    }

    public bool TryKill(string processName, out string? error)
    {
        Process[] processes;
        try
        {
            processes = Process.GetProcessesByName(processName);
        }
        catch (InvalidOperationException ex)
        {
            error = ex.Message;
            _logger.LogWarning($"Failed to enumerate '{processName}' for termination: {ex.Message}");
            return false;
        }

        try
        {
            foreach (var process in processes)
            {
                process.Kill();
                process.WaitForExit(3000);
            }

            error = null;
            return true;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or NotSupportedException)
        {
            error = ex.Message;
            _logger.LogWarning($"Failed to terminate '{processName}': {ex.Message}");
            return false;
        }
        finally
        {
            DisposeAll(processes);
        }
    }

    private static void DisposeAll(Process[] processes)
    {
        foreach (var process in processes)
        {
            process.Dispose();
        }
    }
}
