using Pofus.Core.Logging;
using Pofus.Core.Navigation;
using Pofus.Core.Panels;

namespace Pofus.Core.Tests.Panels;

public class PanelPreferencesStoreTests : IDisposable
{
    private readonly string _tempFile;
    private readonly FakeAppLogger _logger = new();

    public PanelPreferencesStoreTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"pofus-panels-{Guid.NewGuid():N}.json");
    }

    [Fact]
    public async Task LoadAsync_ReturnsEveryPanelVisible_WhenFileDoesNotExist()
    {
        var store = new PanelPreferencesStore(_logger, _tempFile);

        var preferences = await store.LoadAsync();

        Assert.Empty(preferences.Panels);
        Assert.False(preferences.For("switcher").IsHidden);
        Assert.Empty(_logger.Errors);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsHiddenStateAndShortcut()
    {
        var store = new PanelPreferencesStore(_logger, _tempFile);
        var original = new PanelPreferences();
        original.For("switcher").IsHidden = true;
        original.For("switcher").ShowShortcut = new KeyCombo(KeyModifiers.Control | KeyModifiers.Alt, "A", 'A');
        original.For("hud").IsHidden = false;

        await store.SaveAsync(original);
        var loaded = await store.LoadAsync();

        Assert.True(loaded.For("switcher").IsHidden);
        Assert.Equal(original.For("switcher").ShowShortcut, loaded.For("switcher").ShowShortcut);
        Assert.False(loaded.For("hud").IsHidden);
        Assert.Null(loaded.For("hud").ShowShortcut);
    }

    [Fact]
    public async Task LoadAsync_ReturnsDefaultsAndLogsError_WhenFileIsCorrupt()
    {
        await File.WriteAllTextAsync(_tempFile, "{ not valid json ");
        var store = new PanelPreferencesStore(_logger, _tempFile);

        var preferences = await store.LoadAsync();

        Assert.Empty(preferences.Panels);
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
