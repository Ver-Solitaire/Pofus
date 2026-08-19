using System.Text.Json;
using Pofus.Core.Logging;

namespace Pofus.Core.Panels;

public interface IPanelPreferencesStore
{
    Task<PanelPreferences> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(PanelPreferences preferences, CancellationToken cancellationToken = default);
}

/// <summary>
/// Persists <see cref="PanelPreferences"/> to its own local JSON file, kept
/// separate from navigation-shortcuts.json so this feature cannot disturb the
/// already-in-production format of the navigation bindings. Missing/corrupt
/// file falls back to defaults, logged explicitly rather than swallowed
/// (Principe I).
/// </summary>
public sealed class PanelPreferencesStore : IPanelPreferencesStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _filePath;
    private readonly IAppLogger _logger;

    public PanelPreferencesStore(IAppLogger logger, string? filePath = null)
    {
        _logger = logger;
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Pofus", "panels.json");
    }

    public async Task<PanelPreferences> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            _logger.LogInfo($"No panel preferences file at {_filePath}; every window starts visible.");
            return new PanelPreferences();
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var preferences = await JsonSerializer.DeserializeAsync<PanelPreferences>(
                stream, SerializerOptions, cancellationToken);
            return preferences ?? new PanelPreferences();
        }
        catch (JsonException ex)
        {
            _logger.LogError($"Panel preferences file at {_filePath} is corrupt; every window starts visible.", ex);
            return new PanelPreferences();
        }
        catch (IOException ex)
        {
            _logger.LogError($"Failed to read panel preferences file at {_filePath}; every window starts visible.", ex);
            return new PanelPreferences();
        }
    }

    public async Task SaveAsync(PanelPreferences preferences, CancellationToken cancellationToken = default)
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
            _logger.LogError($"Failed to save panel preferences file at {_filePath}.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError($"Access denied writing panel preferences file at {_filePath}.", ex);
        }
    }
}
