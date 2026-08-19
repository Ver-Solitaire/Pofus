using Pofus.Platform;

namespace Pofus.Platform.Tests;

/// <summary>
/// Exercises the real registry Run key, but against a throwaway value name
/// rather than the real "Pofus" entry, so this never touches actual startup
/// behavior on the test machine.
/// </summary>
public class StartupRegistrationTests : IDisposable
{
    private readonly string _valueName = $"PofusTest_{Guid.NewGuid():N}";
    private readonly StartupRegistration _registration;

    public StartupRegistrationTests()
    {
        _registration = new StartupRegistration(new FakeAppLogger(), _valueName, @"C:\fake\pofus.exe");
    }

    [Fact]
    public void IsEnabled_ReturnsFalse_BeforeEnabling()
    {
        Assert.False(_registration.IsEnabled());
    }

    [Fact]
    public void TryEnable_ThenIsEnabled_ReturnsTrue()
    {
        var success = _registration.TryEnable(out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.True(_registration.IsEnabled());
    }

    [Fact]
    public void TryDisable_AfterEnable_ReturnsFalseFromIsEnabled()
    {
        _registration.TryEnable(out _);

        var success = _registration.TryDisable(out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.False(_registration.IsEnabled());
    }

    [Fact]
    public void TryDisable_WhenNeverEnabled_StillSucceeds()
    {
        var success = _registration.TryDisable(out var error);

        Assert.True(success);
        Assert.Null(error);
    }

    public void Dispose() => _registration.TryDisable(out _);

    private sealed class FakeAppLogger : Pofus.Core.Logging.IAppLogger
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
