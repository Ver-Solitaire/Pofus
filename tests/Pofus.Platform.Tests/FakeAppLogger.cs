using Pofus.Core.Logging;

namespace Pofus.Platform.Tests;

internal sealed class FakeAppLogger : IAppLogger
{
    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];
    public List<string> Infos { get; } = [];

    public void LogInfo(string message) => Infos.Add(message);

    public void LogWarning(string message) => Warnings.Add(message);

    public void LogError(string message, Exception? exception = null) => Errors.Add(message);
}
