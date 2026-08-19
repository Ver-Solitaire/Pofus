using Pofus.Core.Logging;
using Pofus.Core.Models;
using Pofus.Core.Persistence;

namespace Pofus.Core.Tests;

public class HudLayoutStoreTests : IDisposable
{
    private readonly string _tempFile;
    private readonly FakeAppLogger _logger = new();

    public HudLayoutStoreTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"pofus-hud-layout-{Guid.NewGuid():N}.json");
    }

    [Fact]
    public async Task LoadAsync_ReturnsDefaultLayout_WhenFileDoesNotExist()
    {
        var store = new HudLayoutStore(_logger, _tempFile);

        var layout = await store.LoadAsync();

        Assert.True(layout.IsVisible);
        Assert.Empty(layout.Slots);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsTheLayout()
    {
        var store = new HudLayoutStore(_logger, _tempFile);
        var original = new HudLayout
        {
            IsVisible = false,
            WindowPosition = new HudPosition { X = 120, Y = 900 },
            Slots =
            [
                new ModuleSlot { SlotId = "accounts", Position = 0, State = ModuleSlotState.Occupied, ModuleId = "accounts" },
                new ModuleSlot { SlotId = "macros", Position = 1 },
            ],
        };

        await store.SaveAsync(original);
        var loaded = await store.LoadAsync();

        Assert.Equal(original.IsVisible, loaded.IsVisible);
        Assert.Equal(original.WindowPosition.X, loaded.WindowPosition.X);
        Assert.Equal(original.WindowPosition.Y, loaded.WindowPosition.Y);
        Assert.Equal(2, loaded.Slots.Count);
        Assert.Equal("accounts", loaded.Slots[0].SlotId);
        Assert.Equal(ModuleSlotState.Occupied, loaded.Slots[0].State);
    }

    [Fact]
    public async Task LoadAsync_ReturnsDefaultLayoutAndLogsError_WhenFileIsCorrupt()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_tempFile)!);
        await File.WriteAllTextAsync(_tempFile, "{ not valid json ");
        var store = new HudLayoutStore(_logger, _tempFile);

        var layout = await store.LoadAsync();

        Assert.Empty(layout.Slots);
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
