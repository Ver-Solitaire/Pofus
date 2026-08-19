using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Pofus.Core.Logging;
using Pofus.Platform;

namespace Pofus.Hud.Switcher;

/// <summary>
/// Turns a Dofus window's taskbar icon into a WPF image for the switcher
/// widget — the same picture the user sees in the Windows taskbar, which for
/// Dofus is the character's class icon.
///
/// Results are cached per HICON because the switcher refreshes every 2s and
/// rebuilding a BitmapSource each time would be pure waste (Principe II).
/// </summary>
public sealed class WindowIconLoader
{
    private readonly IWin32WindowApi _api;
    private readonly IAppLogger _logger;
    private readonly Dictionary<nint, BitmapSource?> _cacheByIconHandle = [];

    public WindowIconLoader(IWin32WindowApi api, IAppLogger logger)
    {
        _api = api;
        _logger = logger;
    }

    /// <summary>The window's icon, or null if it has none or it cannot be read.</summary>
    public BitmapSource? TryLoad(nint windowHandle)
    {
        if (windowHandle == nint.Zero)
        {
            return null;
        }

        var iconHandle = _api.GetWindowIcon(windowHandle);
        if (iconHandle == nint.Zero)
        {
            return null;
        }

        if (_cacheByIconHandle.TryGetValue(iconHandle, out var cached))
        {
            return cached;
        }

        var image = CreateFromHandle(iconHandle);
        _cacheByIconHandle[iconHandle] = image;
        return image;
    }

    private BitmapSource? CreateFromHandle(nint iconHandle)
    {
        try
        {
            var source = Imaging.CreateBitmapSourceFromHIcon(
                iconHandle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            source.Freeze(); // shared across refreshes; freezing also makes it cheap to render
            return source;
        }
        catch (ArgumentException ex)
        {
            // Invalid/stale HICON — fall back to the drawn glyph rather than
            // failing the whole chip (Principe I: logged, never swallowed).
            _logger.LogWarning($"Could not read the icon of a Dofus window (HICON {iconHandle}): {ex.Message}");
            return null;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            _logger.LogWarning($"Win32 error reading the icon of a Dofus window (HICON {iconHandle}): {ex.Message}");
            return null;
        }
    }
}
