using Pofus.Platform;

namespace Pofus.Platform.Tests;

public class DofusWindowLocatorTests
{
    [Fact]
    public void GetOpenDofusWindows_ReturnsEmptyList_WhenNoDofusWindowsAreOpen()
    {
        var api = new FakeWin32WindowApi();
        var resolver = new FakeProcessNameResolver();
        var logger = new FakeAppLogger();
        var locator = new DofusWindowLocator(logger, api, resolver);

        var result = locator.GetOpenDofusWindows();

        Assert.Empty(result);
    }

    [Fact]
    public void GetOpenDofusWindows_ReturnsWindow_WhenVisibleAndOwnedByDofusProcess()
    {
        var api = new FakeWin32WindowApi();
        var handle = new nint(42);
        api.WindowHandles.Add(handle);
        api.Visibility[handle] = true;
        api.ProcessIds[handle] = 1234;
        api.WindowTitles[handle] = "Dofus - MonCompte";

        var resolver = new FakeProcessNameResolver();
        resolver.Names[1234] = "Dofus";

        var locator = new DofusWindowLocator(new FakeAppLogger(), api, resolver);

        var result = locator.GetOpenDofusWindows();

        var window = Assert.Single(result);
        Assert.Equal(handle, window.Handle);
        Assert.Equal("Dofus - MonCompte", window.Title);
    }

    [Fact]
    public void GetOpenDofusWindows_SkipsWindow_WhenNotVisible()
    {
        var api = new FakeWin32WindowApi();
        var handle = new nint(1);
        api.WindowHandles.Add(handle);
        api.Visibility[handle] = false;
        api.ProcessIds[handle] = 1;

        var resolver = new FakeProcessNameResolver();
        resolver.Names[1] = "Dofus";

        var locator = new DofusWindowLocator(new FakeAppLogger(), api, resolver);

        Assert.Empty(locator.GetOpenDofusWindows());
    }

    [Fact]
    public void GetOpenDofusWindows_SkipsWindow_WhenOwningProcessIsNotDofus()
    {
        var api = new FakeWin32WindowApi();
        var handle = new nint(1);
        api.WindowHandles.Add(handle);
        api.Visibility[handle] = true;
        api.ProcessIds[handle] = 1;
        api.WindowTitles[handle] = "Notepad";

        var resolver = new FakeProcessNameResolver();
        resolver.Names[1] = "notepad";

        var locator = new DofusWindowLocator(new FakeAppLogger(), api, resolver);

        Assert.Empty(locator.GetOpenDofusWindows());
    }

    [Fact]
    public void GetOpenDofusWindows_SkipsWindow_WhenOwningProcessHasExited()
    {
        var api = new FakeWin32WindowApi();
        var handle = new nint(1);
        api.WindowHandles.Add(handle);
        api.Visibility[handle] = true;
        api.ProcessIds[handle] = 999;

        var resolver = new FakeProcessNameResolver();

        var locator = new DofusWindowLocator(new FakeAppLogger(), api, resolver);

        Assert.Empty(locator.GetOpenDofusWindows());
    }

    [Fact]
    public void GetOpenDofusWindows_ReturnsEmptyListAndLogsError_WhenEnumerationFails()
    {
        var api = new FakeWin32WindowApi { EnumWindowsSucceeds = false };
        var logger = new FakeAppLogger();
        var locator = new DofusWindowLocator(logger, api, new FakeProcessNameResolver());

        var result = locator.GetOpenDofusWindows();

        Assert.Empty(result);
        Assert.NotEmpty(logger.Errors);
    }

    [Fact]
    public void GetOpenDofusWindows_ReturnsMultipleWindows_ForMultipleDofusAccounts()
    {
        var api = new FakeWin32WindowApi();
        var resolver = new FakeProcessNameResolver();

        for (var i = 1; i <= 8; i++)
        {
            var handle = new nint(i);
            api.WindowHandles.Add(handle);
            api.Visibility[handle] = true;
            api.ProcessIds[handle] = (uint)i;
            api.WindowTitles[handle] = $"Dofus - Compte{i}";
            resolver.Names[(uint)i] = "Dofus";
        }

        var locator = new DofusWindowLocator(new FakeAppLogger(), api, resolver);

        var result = locator.GetOpenDofusWindows();

        Assert.Equal(8, result.Count);
    }

    private sealed class FakeProcessNameResolver : IProcessNameResolver
    {
        public Dictionary<uint, string> Names { get; } = [];

        public string? GetProcessName(uint processId) => Names.GetValueOrDefault(processId);
    }
}
