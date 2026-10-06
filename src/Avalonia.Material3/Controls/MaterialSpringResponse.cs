using Avalonia.Material3.Tokens;

namespace Avalonia.Material3.Controls;

// Pure unit-mass step response. Instant means exactly the target, including t=0.
// Drivers retain their own clocks, geometry, overshoot clamps and settle/lifetime policies.
internal static class MaterialSpringResponse
{
    internal static double Evaluate(double time, MaterialSpring spring)
    {
        if (spring.IsInstant) return 1;
        var omega = Math.Sqrt(spring.Stiffness);
        var damping = spring.DampingRatio;
        if (Math.Abs(damping - 1) < 1e-7)
            return 1 - (1 + omega * time) * Math.Exp(-omega * time);
        if (damping < 1)
        {
            var ratio = Math.Sqrt(1 - damping * damping);
            var phase = omega * ratio * time;
            return 1 - Math.Exp(-damping * omega * time) * (Math.Cos(phase) + damping / ratio * Math.Sin(phase));
        }
        var root = Math.Sqrt(damping * damping - 1);
        var first = -omega * (damping - root);
        var second = -omega * (damping + root);
        return 1 + (second * Math.Exp(first * time) - first * Math.Exp(second * time)) / (first - second);
    }
}
