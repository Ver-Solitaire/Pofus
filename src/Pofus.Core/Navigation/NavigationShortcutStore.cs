using System.Text.Json;
using Pofus.Core.Logging;

namespace Pofus.Core.Navigation;

public interface INavigationShortcutStore
{
    Task<NavigationShortcutPreferences> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(NavigationShortcutPreferences preferences, CancellationToken cancellationToken = default);
}

/// <summary>
/// Persists <see cref="NavigationShortcutPreferences"/> to a local JSON file,
/// separate from other preference files (features 001/002) — same
/// independent-domain pattern. Missing/corrupt file falls back to defaults,
/// logged explicitly rather than swallowed (Principe I).
/// </summary>
public sealed class NavigationShortcutStore : INavigationShortcutStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _filePath;
    private readonly IAppLogger _logger;

    public NavigationShortcutStore(IAppLogger logger, string? filePath = null)
    {
        _logger = logger;
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Pofus", "navigation-shortcuts.json");
    }

    public async Task<NavigationShortcutPreferences> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            _logger.LogInfo($"No navigation shortcuts file at {_filePath}; using defaults.");
            return new NavigationShortcutPreferences();
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var preferences = await JsonSerializer.DeserializeAsync<NavigationShortcutPreferences>(
                stream, SerializerOptions, cancellationToken);
            return preferences ?? new NavigationShortcutPreferences();
        }
        catch (JsonException ex)
        {
            _logger.LogError($"Navigation shortcuts file at {_filePath} is corrupt; using defaults.", ex);
            return new NavigationShortcutPreferences();
        }
        catch (IOException ex)
        {
            _logger.LogError($"Failed to read navigation shortcuts file at {_filePath}; using defaults.", ex);
            return new NavigationShortcutPreferences();
        }
    }

    public async Task SaveAsync(NavigationShortcutPreferences preferences, CancellationToken cancellationToken = default)
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(stream, preferences, SerializerOptions, cancellationToken);
        }
        catch (IOException ex)
        {
            _logger.LogError($"Failed to save navigation shortcuts file at {_filePath}.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError($"Access denied writing navigation shortcuts file at {_filePath}.", ex);
        }
    }
}
