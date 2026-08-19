using Pofus.Core.Accounts;
using Pofus.Core.Logging;

namespace Pofus.Core.Tests.Accounts;

public class AccountPreferencesStoreTests : IDisposable
{
    private readonly string _tempFile;
    private readonly FakeAppLogger _logger = new();

    public AccountPreferencesStoreTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"pofus-account-prefs-{Guid.NewGuid():N}.json");
    }

    [Fact]
    public async Task LoadAsync_ReturnsDefaults_WhenFileDoesNotExist()
    {
        var store = new AccountPreferencesStore(_logger, _tempFile);

        var preferences = await store.LoadAsync();

        Assert.Empty(preferences.ActiveByPseudo);
        Assert.Null(preferences.LeaderPseudo);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsPreferences_KeyedByPseudoNotWindowHandle()
    {
        var store = new AccountPreferencesStore(_logger, _tempFile);
        var original = new AccountPreferences
        {
            LeaderPseudo = "Mon-Perso",
        };
        original.ActiveByPseudo["Mon-Perso"] = false;
        original.TeamByPseudo["Mon-Perso"] = "Équipe Farm";
        original.CustomOrder.Add("Mon-Perso");

        await store.SaveAsync(original);
        var loaded = await store.LoadAsync();

        Assert.False(loaded.ActiveByPseudo["Mon-Perso"]);
        Assert.Equal("Équipe Farm", loaded.TeamByPseudo["Mon-Perso"]);
        Assert.Equal("Mon-Perso", loaded.LeaderPseudo);
        Assert.Equal(["Mon-Perso"], loaded.CustomOrder);
    }

    [Fact]
    public async Task LoadAsync_ReturnsDefaultsAndLogsError_WhenFileIsCorrupt()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_tempFile)!);
        await File.WriteAllTextAsync(_tempFile, "{ not valid json ");
        var store = new AccountPreferencesStore(_logger, _tempFile);

        var preferences = await store.LoadAsync();

        Assert.Empty(preferences.CustomOrder);
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
