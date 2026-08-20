using System.Text.Json;
using Pofus.Core.Logging;
using Pofus.Core.Persistence;

namespace Pofus.Core.Settings;

public interface IAppPreferencesStore
{
    Task<AppPreferences> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AppPreferences preferences, CancellationToken cancellationToken = default);
}

/// <summary>
/// Persists <see cref="AppPreferences"/> to a local JSON file, separate from
/// other preference files — same independent-domain pattern as the rest of
/// the app. Missing/corrupt file falls back to defaults, logged explicitly
/// rather than swallowed (Principe I).
/// </summary>
public sealed class AppPreferencesStore : IAppPreferencesStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _filePath;
    private readonly IAppLogger _logger;

    public AppPreferencesStore(IAppLogger logger, string? filePath = null)
    {
        _logger = logger;
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Pofus", "app-preferences.json");
    }

    public async Task<AppPreferences> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            _logger.LogInfo($"No app preferences file at {_filePath}; using defaults.");
            return new AppPreferences();
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var preferences = await JsonSerializer.DeserializeAsync<AppPreferences>(
                stream, SerializerOptions, cancellationToken);
            return preferences ?? new AppPreferences();
        }
        catch (JsonException ex)
        {
            _logger.LogError($"App preferences file at {_filePath} is corrupt; using defaults.", ex);
            return new AppPreferences();
        }
        catch (IOException ex)
        {
            _logger.LogError($"Failed to read app preferences file at {_filePath}; using defaults.", ex);
            return new AppPreferences();
        }
    }

    public async Task SaveAsync(AppPreferences preferences, CancellationToken cancellationToken = default)
    {
        try
        {
            await JsonFile.WriteAtomicAsync(_filePath, preferences, SerializerOptions, cancellationToken);
        }
        catch (IOException ex)
        {
            _logger.LogError($"Failed to save app preferences file at {_filePath}.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError($"Access denied writing app preferences file at {_filePath}.", ex);
        }
    }
}
