using Avalonia.Material3.Tokens;

namespace Avalonia.Material3.Controls;

// Pure unit-mass step response. Instant means exactly the target, including t=0.
// Drivers retain their own clocks, geometry, overshoot clamps and settle/lifetime policies.
internal static class MaterialSpringResponse
{
    // Unit-mass free response with the sampled velocity retained across a new target.
    internal static (double Value, double Velocity) Sample(double time, double from, double target, double velocity, MaterialSpring spring)
    {
        if (spring.IsInstant) return (target, 0);
        var omega = Math.Sqrt(spring.Stiffness);
        var damping = spring.DampingRatio;
        var displacement = from - target;
        if (Math.Abs(damping - 1) < 1e-7)
        {
            var coefficient = velocity + omega * displacement;
            var decay = Math.Exp(-omega * time);
            return (target + (displacement + coefficient * time) * decay,
                (velocity - omega * coefficient * time) * decay);
        }
        if (damping < 1)
        {
            var rate = damping * omega;
            var frequency = omega * Math.Sqrt(1 - damping * damping);
            var coefficient = (velocity + rate * displacement) / frequency;
            var cosine = Math.Cos(frequency * time);
            var sine = Math.Sin(frequency * time);
            var decay = Math.Exp(-rate * time);
            return (target + decay * (displacement * cosine + coefficient * sine),
                decay * ((frequency * coefficient - rate * displacement) * cosine
                    - (frequency * displacement + rate * coefficient) * sine));
        }
        var root = Math.Sqrt(damping * damping - 1);
        var first = -omega * (damping - root);
        var second = -omega * (damping + root);
        var firstCoefficient = (velocity - second * displacement) / (first - second);
        var secondCoefficient = displacement - firstCoefficient;
        var firstTerm = firstCoefficient * Math.Exp(first * time);
        var secondTerm = secondCoefficient * Math.Exp(second * time);
        return (target + firstTerm + secondTerm, first * firstTerm + second * secondTerm);
    }
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
