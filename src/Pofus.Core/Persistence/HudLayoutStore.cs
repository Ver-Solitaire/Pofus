using System.Text.Json;
using Pofus.Core.Logging;
using Pofus.Core.Models;

namespace Pofus.Core.Persistence;

public interface IHudLayoutStore
{
    Task<HudLayout> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(HudLayout layout, CancellationToken cancellationToken = default);
}

/// <summary>
/// Persists <see cref="HudLayout"/> to a local JSON file only (Principle V — no
/// network transmission). A missing or corrupt file is treated as an explicit,
/// logged condition that falls back to a fresh default layout — never a silent
/// crash or a swallowed exception (Principle I).
/// </summary>
public sealed class HudLayoutStore : IHudLayoutStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _filePath;
    private readonly IAppLogger _logger;

    public HudLayoutStore(IAppLogger logger, string? filePath = null)
    {
        _logger = logger;
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Pofus", "hud-layout.json");
    }

    public async Task<HudLayout> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            _logger.LogInfo($"No HUD layout file at {_filePath}; using default layout.");
            return new HudLayout();
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var layout = await JsonSerializer.DeserializeAsync<HudLayout>(
                stream, SerializerOptions, cancellationToken);
            return layout ?? new HudLayout();
        }
        catch (JsonException ex)
        {
            _logger.LogError($"HUD layout file at {_filePath} is corrupt; using default layout.", ex);
            return new HudLayout();
        }
        catch (IOException ex)
        {
            _logger.LogError($"Failed to read HUD layout file at {_filePath}; using default layout.", ex);
            return new HudLayout();
        }
    }

    public async Task SaveAsync(HudLayout layout, CancellationToken cancellationToken = default)
    {
        try
        {
            await JsonFile.WriteAtomicAsync(_filePath, layout, SerializerOptions, cancellationToken);
        }
        catch (IOException ex)
        {
            _logger.LogError($"Failed to save HUD layout file at {_filePath}.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError($"Access denied writing HUD layout file at {_filePath}.", ex);
        }
    }
}
