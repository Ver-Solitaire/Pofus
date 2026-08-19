using Pofus.Core.Logging;
using Pofus.Core.Settings;

namespace Pofus.Core.Tests.Settings;

public class AppPreferencesStoreTests : IDisposable
{
    private readonly string _tempFile;
    private readonly FakeAppLogger _logger = new();

    public AppPreferencesStoreTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"pofus-app-prefs-{Guid.NewGuid():N}.json");
    }

    [Fact]
    public async Task LoadAsync_ReturnsDefaults_WhenFileDoesNotExist()
    {
        var store = new AppPreferencesStore(_logger, _tempFile);

        var preferences = await store.LoadAsync();

        Assert.False(preferences.IgnoreConflictWarning);
        Assert.False(preferences.LaunchAtStartup);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsBothFlags()
    {
        var store = new AppPreferencesStore(_logger, _tempFile);
        var original = new AppPreferences { IgnoreConflictWarning = true, LaunchAtStartup = true };

        await store.SaveAsync(original);
        var loaded = await store.LoadAsync();

        Assert.True(loaded.IgnoreConflictWarning);
        Assert.True(loaded.LaunchAtStartup);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_CanResetIgnoreConflictWarningBackToFalse()
    {
        var store = new AppPreferencesStore(_logger, _tempFile);
        await store.SaveAsync(new AppPreferences { IgnoreConflictWarning = true });

        await store.SaveAsync(new AppPreferences { IgnoreConflictWarning = false });
        var loaded = await store.LoadAsync();

        Assert.False(loaded.IgnoreConflictWarning);
    }

    [Fact]
    public async Task LoadAsync_ReturnsDefaultsAndLogsError_WhenFileIsCorrupt()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_tempFile)!);
        await File.WriteAllTextAsync(_tempFile, "{ not valid json ");
        var store = new AppPreferencesStore(_logger, _tempFile);

        var preferences = await store.LoadAsync();

        Assert.False(preferences.IgnoreConflictWarning);
        Assert.NotEmpty(_logger.Errors);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    private sealed class FakeAppLogger : IAppLogger
    {
        public List<string> Errors { get; } = [];

        public void LogInfo(string message)
        {
        }

        public void LogWarning(string message)
        {
        }

        public void LogError(string message, Exception? exception = null) => Errors.Add(message);
    }
}
