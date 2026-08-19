using System.Text.Json;
using Pofus.Core.Logging;

namespace Pofus.Core.Appearance;

public interface IAppearanceStore
{
    Task<AppearancePreferences> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AppearancePreferences preferences, CancellationToken cancellationToken = default);
}

/// <summary>
/// Persists <see cref="AppearancePreferences"/> to
/// %APPDATA%\Pofus\appearance.json. A missing or corrupt file falls back to the
/// default look and is logged explicitly rather than swallowed (Principe I,
/// FR-010) — the application must always start.
/// </summary>
public sealed class AppearanceStore : IAppearanceStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _filePath;
    private readonly IAppLogger _logger;

    public AppearanceStore(IAppLogger logger, string? filePath = null)
    {
        _logger = logger;
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Pofus", "appearance.json");
    }

    public async Task<AppearancePreferences> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            _logger.LogInfo($"No appearance file at {_filePath}; using the default look.");
            return AppearancePreferences.CreateDefault();
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var preferences = await JsonSerializer.DeserializeAsync<AppearancePreferences>(
                stream, SerializerOptions, cancellationToken);
            if (preferences is null)
            {
                return AppearancePreferences.CreateDefault();
            }

            // A hand-edited file must not be able to make windows invisible.
            preferences.ClampOpacities();
            return preferences;
        }
        catch (JsonException ex)
        {
            _logger.LogError($"Appearance file at {_filePath} is corrupt; using the default look.", ex);
            return AppearancePreferences.CreateDefault();
        }
        catch (IOException ex)
        {
            _logger.LogError($"Failed to read the appearance file at {_filePath}; using the default look.", ex);
            return AppearancePreferences.CreateDefault();
        }
    }

    public async Task SaveAsync(AppearancePreferences preferences, CancellationToken cancellationToken = default)
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
            _logger.LogError($"Failed to save the appearance file at {_filePath}.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError($"Access denied writing the appearance file at {_filePath}.", ex);
        }
    }
}
