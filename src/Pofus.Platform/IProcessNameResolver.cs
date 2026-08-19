using System.Diagnostics;
using Pofus.Core.Logging;

namespace Pofus.Platform;

/// <summary>Resolves a process name from its id, isolated for testability.</summary>
public interface IProcessNameResolver
{
    /// <summary>Returns the process name, or null if the process no longer exists.</summary>
    string? GetProcessName(uint processId);
}

public sealed class ProcessNameResolver : IProcessNameResolver
{
    private readonly IAppLogger _logger;

    public ProcessNameResolver(IAppLogger logger)
    {
        _logger = logger;
    }

    public string? GetProcessName(uint processId)
    {
        try
        {
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch (ArgumentException)
        {
            _logger.LogInfo($"Process {processId} exited before its name could be resolved.");
            return null;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning($"Could not resolve process {processId}: {ex.Message}");
            return null;
        }
    }
}
