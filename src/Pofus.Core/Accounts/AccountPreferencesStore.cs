using System.Text.Json;
using Pofus.Core.Logging;

namespace Pofus.Core.Accounts;

public interface IAccountPreferencesStore
{
    Task<AccountPreferences> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AccountPreferences preferences, CancellationToken cancellationToken = default);
}

/// <summary>
/// Persists <see cref="AccountPreferences"/> to a local JSON file, separate
/// from hud-layout.json (feature 001) — independent domains. Same
/// missing/corrupt-file handling pattern as <c>HudLayoutStore</c>.
/// </summary>
public sealed class AccountPreferencesStore : IAccountPreferencesStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _filePath;
    private readonly IAppLogger _logger;

    public AccountPreferencesStore(IAppLogger logger, string? filePath = null)
    {
        _logger = logger;
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Pofus", "account-preferences.json");
    }

    public async Task<AccountPreferences> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            _logger.LogInfo($"No account preferences file at {_filePath}; using defaults.");
            return new AccountPreferences();
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var preferences = await JsonSerializer.DeserializeAsync<AccountPreferences>(
                stream, SerializerOptions, cancellationToken);
            return preferences ?? new AccountPreferences();
        }
        catch (JsonException ex)
        {
            _logger.LogError($"Account preferences file at {_filePath} is corrupt; using defaults.", ex);
            return new AccountPreferences();
        }
        catch (IOException ex)
        {
            _logger.LogError($"Failed to read account preferences file at {_filePath}; using defaults.", ex);
            return new AccountPreferences();
        }
    }

    public async Task SaveAsync(AccountPreferences preferences, CancellationToken cancellationToken = default)
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
            _logger.LogError($"Failed to save account preferences file at {_filePath}.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError($"Access denied writing account preferences file at {_filePath}.", ex);
        }
    }
}
