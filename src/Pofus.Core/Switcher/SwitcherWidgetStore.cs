using System.Text.Json;
using Pofus.Core.Logging;

namespace Pofus.Core.Switcher;

public interface ISwitcherWidgetStore
{
    Task<SwitcherWidgetPreferences> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(SwitcherWidgetPreferences preferences, CancellationToken cancellationToken = default);
}

/// <summary>
/// Persists <see cref="SwitcherWidgetPreferences"/> to a local JSON file,
/// separate from other preference files — same independent-domain pattern
/// as the rest of the app. Missing/corrupt file falls back to defaults,
/// logged explicitly rather than swallowed (Principe I).
/// </summary>
public sealed class SwitcherWidgetStore : ISwitcherWidgetStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _filePath;
    private readonly IAppLogger _logger;

    public SwitcherWidgetStore(IAppLogger logger, string? filePath = null)
    {
        _logger = logger;
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Pofus", "account-switcher.json");
    }

    public async Task<SwitcherWidgetPreferences> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            _logger.LogInfo($"No account switcher preferences file at {_filePath}; using defaults.");
            return new SwitcherWidgetPreferences();
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var preferences = await JsonSerializer.DeserializeAsync<SwitcherWidgetPreferences>(
                stream, SerializerOptions, cancellationToken);
            return preferences ?? new SwitcherWidgetPreferences();
        }
        catch (JsonException ex)
        {
            _logger.LogError($"Account switcher preferences file at {_filePath} is corrupt; using defaults.", ex);
            return new SwitcherWidgetPreferences();
        }
        catch (IOException ex)
        {
            _logger.LogError($"Failed to read account switcher preferences file at {_filePath}; using defaults.", ex);
            return new SwitcherWidgetPreferences();
        }
    }

    public async Task SaveAsync(SwitcherWidgetPreferences preferences, CancellationToken cancellationToken = default)
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
            _logger.LogError($"Failed to save account switcher preferences file at {_filePath}.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError($"Access denied writing account switcher preferences file at {_filePath}.", ex);
        }
    }
}
