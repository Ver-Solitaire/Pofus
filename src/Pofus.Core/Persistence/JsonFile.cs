using System.Text.Json;

namespace Pofus.Core.Persistence;

/// <summary>
/// Shared write path for the preference files in %APPDATA%\Pofus.
/// </summary>
public static class JsonFile
{
    /// <summary>
    /// Serialises to a sibling temporary file, then replaces the target in one
    /// move.
    ///
    /// Writing straight into the target truncates it first, so anything that
    /// interrupts the write — a crash, a forced quit, the process exiting while
    /// a save started from a Closing handler was still in flight — left a
    /// half-written file behind, and the user lost that preference set. The move
    /// is atomic on NTFS: the file on disk is either the previous content or the
    /// new one, never a fragment.
    /// </summary>
    public static async Task WriteAtomicAsync<T>(
        string filePath,
        T value,
        JsonSerializerOptions options,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = filePath + ".tmp";

        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, value, options, cancellationToken);
        }

        File.Move(temporaryPath, filePath, overwrite: true);
    }
}
