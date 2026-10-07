using System;

namespace Avalonia.Material3.Controls;

// AndroidX SplineBasedDecay.kt/FlingCalculator.kt, pinned11ece46a49d485c7644e53cb0684a611d7a0ec10.
// Copyright2020 The Android Open Source Project; Apache License2.0.
// The density factors cancel when both velocity and distance are expressed in DIP.
internal readonly struct MaterialSplineDecay
{
    private static readonly double[] Positions = CreatePositions();
    private static readonly double DecelerationRate = Math.Log(.78) / Math.Log(.9);
    private const double Friction = .015 * 9.80665 * 39.37 * 160 * .84;
    public double Duration { get; }
    public double Distance { get; }
    public MaterialSplineDecay(double velocity)
    {
        var exponent = Math.Log(.35 * Math.Abs(velocity) / Friction);
        Duration = velocity == 0 ? 0 : Math.Floor(1000 * Math.Exp(exponent / (DecelerationRate - 1))) / 1000;
        Distance = velocity == 0 ? 0 : Math.Sign(velocity) * Friction * Math.Exp(DecelerationRate / (DecelerationRate - 1) * exponent);
    }
    public (double Position, double Velocity) Sample(double seconds)
    {
        if (Duration <= 0 || seconds >= Duration) return (Distance, 0);
        var time = Math.Clamp(seconds / Duration, 0, 1);
        var index = Math.Min(99, (int)(time * 100));
        var velocity = (Positions[index + 1] - Positions[index]) * 100;
        return (Distance * (Positions[index] + (time - index / 100d) * velocity), Distance * velocity / Duration);
    }
    private static double[] CreatePositions()
    {
        var positions = new double[101]; var minimum = 0d;
        for (var i = 0; i < 100; i++)
        {
            var alpha = i / 100d; var maximum = 1d;
            double x, coefficient, time;
            do
            {
                x = (minimum + maximum) / 2;
                coefficient = 3 * x * (1 - x);
                time = coefficient * ((1 - x) * .175 + x * .35) + x * x * x;
                if (time > alpha) maximum = x; else minimum = x;
            } while (Math.Abs(time - alpha) >= 1e-5);
            positions[i] = coefficient * ((1 - x) * .5 + x) + x * x * x;
        }
        positions[100] = 1; return positions;
    }
}
