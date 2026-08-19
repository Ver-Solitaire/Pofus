namespace Pofus.Core.Settings;

/// <summary>Persisted general app preferences (%APPDATA%\Pofus\app-preferences.json).</summary>
public sealed class AppPreferences
{
    public bool IgnoreConflictWarning { get; set; }

    public bool LaunchAtStartup { get; set; }
}
