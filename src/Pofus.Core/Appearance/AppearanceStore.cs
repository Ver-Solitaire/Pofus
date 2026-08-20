using System.Text.Json;
using Pofus.Core.Logging;
using Pofus.Core.Persistence;

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
            AppearancePreferences? preferences;

            // Scoped so the read handle is closed before the migration below can
            // rewrite the same path.
            await using (var stream = File.OpenRead(_filePath))
            {
                preferences = await JsonSerializer.DeserializeAsync<AppearancePreferences>(
                    stream, SerializerOptions, cancellationToken);
            }

            if (preferences is null)
            {
                return AppearancePreferences.CreateDefault();
            }

            // A hand-edited file must not be able to make windows invisible.
            preferences.ClampOpacities();

            if (preferences.UpgradeLegacyTextColor())
            {
                _logger.LogInfo(
                    "Couleur de texte passée du blanc cassé au blanc pur (nouveau réglage " +
                    "par défaut). Elle reste modifiable dans les réglages.");

                // Written back straight away so the migration runs once instead
                // of announcing itself on every start.
                await SaveAsync(preferences, cancellationToken);
            }

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
            await JsonFile.WriteAtomicAsync(_filePath, preferences, SerializerOptions, cancellationToken);
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
