using System;

namespace NotchBeat;

/// <summary>
/// Turns scrolling over the expanded island into actions:
/// scroll sideways (or swipe two fingers on a touchpad) to skip tracks, scroll up/down to change volume.
/// </summary>
public sealed class ScrollGestures
{
    /// <summary>A pause this long starts a new swipe.</summary>
    private static readonly TimeSpan SwipeGap = TimeSpan.FromSeconds(0.4);

    private readonly IslandModel _model;
    private double _swipeDistance;
    private bool _didSkipThisSwipe;
    private DateTime _lastHorizontal = DateTime.MinValue;

    public ScrollGestures(IslandModel model) => _model = model;

    /// <param name="delta">Wheel units: 120 per mouse-wheel notch, positive when scrolling up.</param>
    public void Vertical(int delta)
    {
        if (_model.Mode != IslandMode.Expanded || delta == 0) return;
        _model.AdjustVolume(delta / 120.0 * Theme.Gestures.VolumePerWheelStep); // Up = louder.
    }

    /// <param name="delta">Wheel units, positive when scrolling right.</param>
    public void Horizontal(int delta)
    {
        if (_model.Mode != IslandMode.Expanded || delta == 0) return;

        var now = DateTime.UtcNow;
        if (now - _lastHorizontal > SwipeGap)
        {
            _swipeDistance = 0;
            _didSkipThisSwipe = false;
        }
        _lastHorizontal = now;

        _swipeDistance += delta;
        if (!_didSkipThisSwipe && Math.Abs(_swipeDistance) >= Theme.Gestures.SwipeThreshold)
        {
            _didSkipThisSwipe = true;
            _model.NowPlaying.Perform(_swipeDistance > 0 ? PlayerCommand.Next : PlayerCommand.Previous);
        }
    }
}
