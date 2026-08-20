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
///
/// The icons carry no alpha channel: the artwork arrives already composited
/// over black, and it is drawn to be read that way — it has a white outline
/// stroke, and the WebP chroma subsampling leaves coloured speckles along the
/// silhouette. Cutting the black out exposes both, so the views put the icons
/// on a black backdrop instead of fighting the asset.
/// </summary>
public sealed class ItemImageLoader
{
    // DofusBook publishes two sizes; the larger one is what the tile view needs
    // if it is not to look upscaled.
    private const string SmallUrlFormat = "https://www.dofusbook.net/static/dist/items/{0}-50.webp";
    private const string LargeUrlFormat = "https://www.dofusbook.net/static/dist/items/{0}-70.webp";

    private readonly IAppLogger _logger;
    private readonly Dictionary<(int Picture, bool Large), BitmapImage?> _cache = [];
    private bool _decodeFailureReported;

    public ItemImageLoader(IAppLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// The icon for a picture id, or null if there is none to show.
    /// <paramref name="large"/> selects the tile-sized artwork.
    /// </summary>
    public BitmapImage? TryGet(int pictureId, bool large = false)
    {
        if (pictureId <= 0)
        {
            return null;
        }

        var key = (pictureId, large);
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        BitmapImage? image = null;
        try
        {
            image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(
                string.Format(large ? LargeUrlFormat : SmallUrlFormat, pictureId), UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            // Decode at the size actually rendered rather than full size.
            image.DecodePixelWidth = large ? 70 : 24;
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

        _cache[key] = image;
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
