namespace NotchBeat;

/// <summary>A short system event the island pops up for, like a volume change or headphones connecting.</summary>
public sealed record SystemActivity(SystemActivity.ActivityKind Kind, string Glyph, string Title, double? Level = null)
{
    public enum ActivityKind
    {
        Volume,
        OutputDevice,
        Power,
    }
}
