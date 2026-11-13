namespace NotchBeat;

/// <summary>The look of the open island. Picked in Settings.</summary>
public enum IslandTheme
{
    /// <summary>Blurred album art behind the player, waves along the bottom.</summary>
    NowBar,
    /// <summary>Pure black and minimal, no bottom waves.</summary>
    Classic,
    /// <summary>Black with a glowing outline in the album's color.</summary>
    Neon,
    /// <summary>Frosted, see-through glass.</summary>
    Glass,
}

public static class IslandThemeExtensions
{
    public static string Title(this IslandTheme theme) => theme switch
    {
        IslandTheme.NowBar => "Now Bar",
        IslandTheme.Classic => "Classic",
        IslandTheme.Neon => "Neon",
        IslandTheme.Glass => "Glass",
        _ => theme.ToString(),
    };

    public static bool ShowsBottomWaves(this IslandTheme theme) => theme != IslandTheme.Classic;
}
