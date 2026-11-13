using System;

namespace NotchBeat;

/// <summary>A value that springs toward its target, like SwiftUI's spring animations.</summary>
public sealed class Spring
{
    public double Value { get; private set; }
    public double Target { get; set; }
    private double _velocity;

    public Spring(double value)
    {
        Value = value;
        Target = value;
    }

    public bool IsSettled => Math.Abs(Value - Target) < 0.01 && Math.Abs(_velocity) < 0.01;

    /// <param name="response">Roughly how long one bounce takes, in seconds.</param>
    /// <param name="damping">1 settles without overshoot; lower values bounce.</param>
    public void Step(double seconds, double response, double damping)
    {
        if (IsSettled)
        {
            Value = Target;
            _velocity = 0;
            return;
        }

        double stiffness = Math.Pow(2 * Math.PI / response, 2);
        double friction = 4 * Math.PI * damping / response;
        // Small fixed steps keep the motion stable even when a frame is slow.
        seconds = Math.Min(seconds, 0.1);
        while (seconds > 0)
        {
            double step = Math.Min(seconds, 1 / 240.0);
            double acceleration = -stiffness * (Value - Target) - friction * _velocity;
            _velocity += acceleration * step;
            Value += _velocity * step;
            seconds -= step;
        }
    }
}
