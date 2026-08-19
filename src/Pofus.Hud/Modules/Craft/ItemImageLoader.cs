using System.Windows.Media.Imaging;
using Pofus.Core.Logging;

namespace Pofus.Hud.Modules.Craft;

/// <summary>
/// Item icons, taken from the same place the workshop page itself uses:
/// <c>/static/dist/items/{picture}-50.webp</c>.
///
/// WPF downloads and decodes these on its own once given the URI, so nothing
/// blocks the UI thread. Decoding relies on the WebP codec Windows exposes
/// through WIC — DofusBook publishes no other format for these icons — and a
/// missing codec or a failed download simply leaves the row without an image
/// rather than breaking the list.
/// </summary>
public sealed class ItemImageLoader
{
    private const string IconUrlFormat = "https://www.dofusbook.net/static/dist/items/{0}-50.webp";

    private readonly IAppLogger _logger;
    private readonly Dictionary<int, BitmapImage?> _cacheByPicture = [];
    private bool _decodeFailureReported;

    public ItemImageLoader(IAppLogger logger)
    {
        _logger = logger;
    }

    /// <summary>The icon for a picture id, or null if there is none to show.</summary>
    public BitmapImage? TryGet(int pictureId)
    {
        if (pictureId <= 0)
        {
            return null;
        }

        if (_cacheByPicture.TryGetValue(pictureId, out var cached))
        {
            return cached;
        }

        BitmapImage? image = null;
        try
        {
            image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(string.Format(IconUrlFormat, pictureId), UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = 24; // rendered small; decode small
            image.EndInit();

            // Loading is asynchronous, so failures arrive as events rather than
            // exceptions. Report once — one broken icon is not worth a log flood.
            image.DecodeFailed += OnImageFailed;
            image.DownloadFailed += OnImageFailed;
        }
        catch (Exception ex) when (ex is UriFormatException or NotSupportedException or InvalidOperationException)
        {
            _logger.LogWarning($"Could not prepare the icon for picture {pictureId}: {ex.Message}");
            image = null;
        }

        _cacheByPicture[pictureId] = image;
        return image;
    }

    private void OnImageFailed(object? sender, System.Windows.Media.ExceptionEventArgs e)
    {
        if (_decodeFailureReported)
        {
            return;
        }

        _decodeFailureReported = true;
        _logger.LogWarning(
            "Item icons could not be loaded (missing WebP codec or network issue); "
            + $"the list is shown without them. First error: {e.ErrorException.Message}");
    }
}
