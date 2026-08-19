using Pofus.Core.Accounts;
using Pofus.Core.Logging;
using Pofus.Core.Models;
using Pofus.Core.Platform;

namespace Pofus.Core.Tests.Accounts;

public class AccountDetectionServiceTests
{
    [Fact]
    public async Task RefreshAsync_ReturnsEmptyList_WhenNoWindowsDetected()
    {
        var service = CreateService(new FakeDofusWindowLocator());

        var result = await service.RefreshAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task RefreshAsync_ExcludesLauncherWindows()
    {
        var locator = new FakeDofusWindowLocator();
        locator.Windows.Add(new DofusWindowInfo(1, "Dofus"));

        var result = await CreateService(locator).RefreshAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task RefreshAsync_AppliesPersistedActiveState_KeyedByPseudo()
    {
        var locator = new FakeDofusWindowLocator();
        locator.Windows.Add(new DofusWindowInfo(1, "Mon-Perso - Ouginak"));
        var store = new InMemoryAccountPreferencesStore();
        store.Preferences.ActiveByPseudo["Mon-Perso"] = false;

        var result = await CreateService(locator, store).RefreshAsync();

        var account = Assert.Single(result);
        Assert.False(account.IsActive);
        Assert.Equal("Ouginak", account.ClassName);
    }

    [Fact]
    public async Task RefreshAsync_KeepsOnlyFirstAccount_WhenPseudoIsDuplicated()
    {
        var locator = new FakeDofusWindowLocator();
        locator.Windows.Add(new DofusWindowInfo(1, "MemePseudo - Iop"));
        locator.Windows.Add(new DofusWindowInfo(2, "MemePseudo - Cra"));

        var result = await CreateService(locator).RefreshAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task RefreshAsync_KeepsLeaderStatus_AfterAccountDisappears()
    {
        var store = new InMemoryAccountPreferencesStore();
        store.Preferences.LeaderPseudo = "Mon-Perso";

        // First refresh: leader's window is open.
        var locator = new FakeDofusWindowLocator();
        locator.Windows.Add(new DofusWindowInfo(1, "Mon-Perso - Ouginak"));
        var service = CreateService(locator, store);
        var withLeader = await service.RefreshAsync();
        Assert.True(Assert.Single(withLeader).IsLeader);

        // Second refresh: leader disconnects — LeaderPseudo must not change.
        locator.Windows.Clear();
        var withoutLeader = await service.RefreshAsync();

        Assert.Empty(withoutLeader);
        Assert.Equal("Mon-Perso", store.Preferences.LeaderPseudo);
    }

    [Fact]
    public async Task RefreshAsync_OrdersAccounts_AccordingToCustomOrder()
    {
        var store = new InMemoryAccountPreferencesStore();
        store.Preferences.CustomOrder.AddRange(["Second", "First"]);

        var locator = new FakeDofusWindowLocator();
        locator.Windows.Add(new DofusWindowInfo(1, "First - Iop"));
        locator.Windows.Add(new DofusWindowInfo(2, "Second - Cra"));

        var result = await CreateService(locator, store).RefreshAsync();

        Assert.Equal(["Second", "First"], result.Select(a => a.Pseudo));
    }

    private static AccountDetectionService CreateService(
        FakeDofusWindowLocator locator, InMemoryAccountPreferencesStore? store = null) =>
        new(locator, store ?? new InMemoryAccountPreferencesStore(), new NullAppLogger());

    private sealed class FakeDofusWindowLocator : IDofusWindowLocator
    {
        public List<DofusWindowInfo> Windows { get; } = [];

        public IReadOnlyList<DofusWindowInfo> GetOpenDofusWindows() => Windows;
    }

    private sealed class InMemoryAccountPreferencesStore : IAccountPreferencesStore
    {
        public AccountPreferences Preferences { get; set; } = new();

        public Task<AccountPreferences> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Preferences);

        public Task SaveAsync(AccountPreferences preferences, CancellationToken cancellationToken = default)
        {
            Preferences = preferences;
            return Task.CompletedTask;
        }
    }

    private sealed class NullAppLogger : IAppLogger
    {
        public void LogInfo(string message)
        {
        }

        public void LogWarning(string message)
        {
        }

        public void LogError(string message, Exception? exception = null)
        {
        }
    }
}
