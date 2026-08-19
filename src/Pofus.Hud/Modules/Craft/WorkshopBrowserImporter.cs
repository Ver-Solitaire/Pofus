using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Pofus.Core.Craft;
using Pofus.Core.Logging;

namespace Pofus.Hud.Modules.Craft;

/// <summary>
/// Loads a DofusBook workshop URL in an embedded Edge/Chromium view and reads
/// the equipment list out of the page.
///
/// Why a real browser rather than an HTTP call: the workshop is a Vue app whose
/// data comes from dofusbook.net/api/, and that API refuses plain HTTP clients
/// (Cloudflare, "you have been blocked"). Nothing here forges a fingerprint or
/// defeats a challenge — WebView2 *is* a browser, so it is served like one, and
/// if Cloudflare ever does present a human check the user sees it and answers
/// it themselves.
/// </summary>
public sealed class WorkshopBrowserImporter
{
    /// <summary>Long enough for a slow SPA plus a possible Cloudflare interstitial.</summary>
    private static readonly TimeSpan LoadTimeout = TimeSpan.FromSeconds(25);

    private readonly IAppLogger _logger;

    public WorkshopBrowserImporter(IAppLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Only DofusBook hosts are ever navigated to, so a pasted link cannot turn
    /// this into a general-purpose fetcher for arbitrary URLs.
    /// </summary>
    public static bool IsSupportedWorkshopUrl(string? url, out Uri? normalized)
    {
        normalized = null;
        if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        var allowed = host is "d-bk.net" or "dofusbook.net"
            || host.EndsWith(".dofusbook.net", StringComparison.Ordinal);
        if (!allowed)
        {
            return false;
        }

        normalized = uri;
        return true;
    }

    /// <summary>
    /// Loads the workshop and extracts its equipment. Throws
    /// <see cref="CraftDataUnavailableException"/> with a user-facing message on
    /// any failure — never leaves the caller guessing.
    /// </summary>
    public async Task<IReadOnlyList<WorkshopCraft>> ImportAsync(Uri workshopUrl)
    {
        // WebView2 renders into its own child HWND and ignores WPF clipping and
        // z-order ("airspace"), so it cannot be hidden inside a normal window —
        // it would paint the web page over everything. It therefore lives in a
        // throwaway window parked far off-screen.
        var host = new WebView2 { Width = 1024, Height = 768 };
        var carrier = new Window
        {
            Width = 1024,
            Height = 768,
            Left = -32000,
            Top = -32000,
            ShowInTaskbar = false,
            ShowActivated = false,
            WindowStyle = WindowStyle.None,
            Content = host,
        };

        try
        {
            carrier.Show();
            return await RunImportAsync(host, workshopUrl);
        }
        finally
        {
            carrier.Close();
            host.Dispose();
        }
    }

    private async Task<IReadOnlyList<WorkshopCraft>> RunImportAsync(WebView2 host, Uri workshopUrl)
    {
        await EnsureInitializedAsync(host);

        var navigation = new TaskCompletionSource<bool>();
        void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e) =>
            navigation.TrySetResult(e.IsSuccess);

        host.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
        try
        {
            host.CoreWebView2.Navigate(workshopUrl.ToString());
            var completed = await Task.WhenAny(navigation.Task, Task.Delay(LoadTimeout));
            if (completed != navigation.Task)
            {
                throw new CraftDataUnavailableException(
                    "La page DofusBook met trop de temps à répondre. Réessayez, ou terminez la vérification affichée.");
            }

            if (!await navigation.Task)
            {
                throw new CraftDataUnavailableException(
                    "La page DofusBook n'a pas pu être chargée. Vérifiez le lien et votre connexion.");
            }
        }
        finally
        {
            host.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
        }

        return await ExtractWithRetriesAsync(host);
    }

    /// <summary>
    /// Starts the in-page lookup, then polls until it reports back. The SPA
    /// needs a moment to boot before its API is reachable, so a null answer is
    /// retried rather than treated as failure.
    /// </summary>
    private async Task<IReadOnlyList<WorkshopCraft>> ExtractWithRetriesAsync(WebView2 host)
    {
        const int kickoffAttempts = 6;
        const int pollAttempts = 30; // ~21s once started

        for (var kickoff = 0; kickoff < kickoffAttempts; kickoff++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(kickoff == 0 ? 400 : 1500));
            await RunScriptAsync(host, WorkshopExtractionScript.Kickoff);

            for (var poll = 0; poll < pollAttempts; poll++)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(700));

                var raw = await RunScriptAsync(host, WorkshopExtractionScript.Poll);
                if (string.IsNullOrWhiteSpace(raw) || raw == "null")
                {
                    continue; // still working
                }

                var crafts = TryReadResult(raw);
                if (crafts is { Count: > 0 })
                {
                    return crafts;
                }

                break; // reported an empty result — let the page settle, then retry
            }
        }

        throw new CraftDataUnavailableException(
            "Aucun équipement n'a pu être lu depuis cet atelier. Vérifiez que le lien pointe bien vers "
            + "un atelier contenant des équipements avec une recette.");
    }


    private async Task<string> RunScriptAsync(WebView2 host, string script)
    {
        try
        {
            return await host.CoreWebView2.ExecuteScriptAsync(script);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("The embedded browser rejected the extraction script.", ex);
            throw new CraftDataUnavailableException("Le navigateur intégré n'a pas répondu. Réessayez.", ex);
        }
    }

    /// <summary>ExecuteScriptAsync returns the result as a JSON-encoded string.</summary>
    private IReadOnlyList<WorkshopCraft>? TryReadResult(string scriptResult)
    {
        if (string.IsNullOrWhiteSpace(scriptResult) || scriptResult == "null")
        {
            return null;
        }

        try
        {
            // The script yields a string, which arrives JSON-quoted: unwrap once.
            var inner = JsonSerializer.Deserialize<string>(scriptResult);
            return WorkshopParser.TryParse(inner, out _);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning($"Unreadable extraction payload from the workshop page: {ex.Message}");
            return null;
        }
    }

    private async Task EnsureInitializedAsync(WebView2 host)
    {
        if (host.CoreWebView2 is not null)
        {
            return;
        }

        try
        {
            // Keep the profile beside Pofus's own data instead of next to the
            // executable, which may sit in a read-only location.
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Pofus", "browser");
            Directory.CreateDirectory(userDataFolder);

            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
            await host.EnsureCoreWebView2Async(environment);
        }
        catch (WebView2RuntimeNotFoundException ex)
        {
            _logger.LogError("The WebView2 runtime is missing.", ex);
            throw new CraftDataUnavailableException(
                "Le composant WebView2 de Windows est introuvable. Installez « Microsoft Edge WebView2 Runtime ».", ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _logger.LogError("Could not start the embedded browser.", ex);
            throw new CraftDataUnavailableException("Impossible de démarrer le navigateur intégré.", ex);
        }
    }
}
