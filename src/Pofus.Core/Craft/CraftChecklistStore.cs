using System.Text.Json;
using Pofus.Core.Logging;
using Pofus.Core.Persistence;

namespace Pofus.Core.Craft;

/// <summary>
/// Which resources the user has already gathered, plus the last imported
/// workshop so the list survives a restart (%APPDATA%\Pofus\craft.json).
/// Checked state is keyed on item id, so re-importing the same workshop keeps
/// the ticks even if names or quantities changed.
/// </summary>
public sealed class CraftState
{
    /// <summary>
    /// The workshop this list came from, so it can be re-read later without
    /// asking the user to paste the link again.
    /// </summary>
    public string? WorkshopUrl { get; set; }

    public List<CraftItem> Equipment { get; set; } = [];

    public List<RequiredResource> Resources { get; set; } = [];

    public HashSet<int> GatheredItemIds { get; set; } = [];

    /// <summary>
    /// Equipment already crafted, marked by clicking its tile. Kept apart from
    /// <see cref="GatheredItemIds"/>: buying a resource and having finished the
    /// item are two different milestones, and an equipment id can collide with
    /// a resource id.
    /// </summary>
    public HashSet<int> CraftedItemIds { get; set; } = [];
}

public interface ICraftStateStore
{
    Task<CraftState> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(CraftState state, CancellationToken cancellationToken = default);
}

public sealed class CraftStateStore : ICraftStateStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _filePath;
    private readonly IAppLogger _logger;

    public CraftStateStore(IAppLogger logger, string? filePath = null)
    {
        _logger = logger;
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Pofus", "craft.json");
    }

    public async Task<CraftState> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            _logger.LogInfo($"No craft state file at {_filePath}; starting with an empty list.");
            return new CraftState();
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            return await JsonSerializer.DeserializeAsync<CraftState>(stream, SerializerOptions, cancellationToken)
                ?? new CraftState();
        }
        catch (JsonException ex)
        {
            _logger.LogError($"Craft state file at {_filePath} is corrupt; starting with an empty list.", ex);
            return new CraftState();
        }
        catch (IOException ex)
        {
            _logger.LogError($"Failed to read the craft state file at {_filePath}; starting with an empty list.", ex);
            return new CraftState();
        }
    }

    public async Task SaveAsync(CraftState state, CancellationToken cancellationToken = default)
    {
        try
        {
            await JsonFile.WriteAtomicAsync(_filePath, state, SerializerOptions, cancellationToken);
        }
        catch (IOException ex)
        {
            _logger.LogError($"Failed to save the craft state file at {_filePath}.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError($"Access denied writing the craft state file at {_filePath}.", ex);
        }
    }
}
