namespace Pofus.Core.Switcher;

public sealed class SwitcherWidgetPosition
{
    public double X { get; set; }

    public double Y { get; set; }
}

/// <summary>Persisted screen position of the detachable account switcher widget.</summary>
public sealed class SwitcherWidgetPreferences
{
    public SwitcherWidgetPosition Position { get; set; } = new();
}
