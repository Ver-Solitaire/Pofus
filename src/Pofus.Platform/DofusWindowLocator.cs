using Pofus.Core.Logging;
using Pofus.Core.Models;
using Pofus.Core.Platform;

namespace Pofus.Platform;

public sealed class DofusWindowLocator : IDofusWindowLocator
{
    private const string DofusProcessNameFragment = "dofus";

    private readonly IWin32WindowApi _api;
    private readonly IProcessNameResolver _processNameResolver;
    private readonly IAppLogger _logger;

    public DofusWindowLocator(IAppLogger logger)
        : this(logger, new Win32WindowApi(), new ProcessNameResolver(logger))
    {
    }

    public DofusWindowLocator(IAppLogger logger, IWin32WindowApi api, IProcessNameResolver processNameResolver)
    {
        _logger = logger;
        _api = api;
        _processNameResolver = processNameResolver;
    }

    public IReadOnlyList<DofusWindowInfo> GetOpenDofusWindows()
    {
        var results = new List<DofusWindowInfo>();

        bool EnumCallback(nint hWnd, nint lParam)
        {
            TryAddDofusWindow(hWnd, results);
            return true;
        }

        if (!_api.EnumWindows(EnumCallback))
        {
            var error = _api.GetLastWin32Error();
            _logger.LogError($"EnumWindows failed with Win32 error {error}; returning no Dofus windows.");
            return [];
        }

        return results;
    }

    private void TryAddDofusWindow(nint hWnd, List<DofusWindowInfo> results)
    {
        if (!_api.IsWindowVisible(hWnd))
        {
            return;
        }

        _api.GetWindowThreadProcessId(hWnd, out var processId);
        var processName = _processNameResolver.GetProcessName(processId);
        if (processName is null ||
            !processName.Contains(DofusProcessNameFragment, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var title = _api.GetWindowText(hWnd);
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        results.Add(new DofusWindowInfo(hWnd, title));
    }
}
