// Shape definitions adapted from AndroidX MaterialShapes.kt, commit
// 11ece46a49d485c7644e53cb0684a611d7a0ec10. Copyright 2024 The Android Open Source Project.
// Licensed under the Apache License, Version 2.0 (https://www.apache.org/licenses/LICENSE-2.0).
namespace Avalonia.Material3.Controls;

// The pinned vertices/radii are rounded, then sampled on equal angular rays. This bounded
// Avalonia morph projection preserves silhouettes; it is not AndroidX's feature-matching Morph engine.
internal static class MaterialLoadingShapes
{
    private const int Samples = 240;
    private readonly record struct Corner(Vector Point, double Radius);
    internal static readonly Vector[] Circle = Enumerable.Range(0, Samples).Select(i => Unit(i * Math.Tau / Samples)).ToArray();
    internal static readonly Vector[][] Cycle =
    [
        Polygon(Repeat([C(.193,.277,.053), C(.176,.055,.053)], 10)), // SoftBurst
        Polygon(Star(9, .8, .5, -Math.PI / 2)),                    // Cookie9Sided
        Polygon(Repeat([C(.5,-.009,.172), C(1.030,.365,.164), C(.828,.970,.169)], 1, true)), // Pentagon
        Polygon(Repeat([C(.961,.039,.426), C(1.001,.428,0), C(1,.609,1)], 2, true)), // Pill
        Polygon(Star(8, .8, .15, 0)),                             // Sunny
        Polygon(Repeat([C(1.237,1.236,.258), C(.5,.918,.233)], 4)),  // Cookie4Sided
        Enumerable.Range(0, Samples).Select(i =>
        {
            var angle = i * Math.Tau / Samples;
            var local = angle + Math.PI / 4;
            var radius = 1 / Math.Sqrt(Math.Pow(Math.Cos(local), 2) + Math.Pow(Math.Sin(local) / .64, 2));
            return Unit(angle) * radius;
        }).ToArray()                                            // Oval, rotated -45 degrees
    ];
    private static Corner C(double x, double y, double radius) => new(new Vector(x - .5, y - .5), radius);
    private static Vector Unit(double angle) => new(Math.Cos(angle), Math.Sin(angle));
    private static Vector Rotate(Vector point, double angle) => new(point.X * Math.Cos(angle) - point.Y * Math.Sin(angle), point.X * Math.Sin(angle) + point.Y * Math.Cos(angle));
    private static Corner[] Star(int points, double innerRadius, double rounding, double angle) => Enumerable.Range(0, points * 2)
        .Select(i => new Corner(Unit(angle + i * Math.Tau / (points * 2)) * (i % 2 == 0 ? 1 : innerRadius), rounding)).ToArray();
    private static Corner[] Repeat(Corner[] source, int repetitions, bool mirror = false)
    {
        var output = new List<Corner>();
        if (!mirror)
        {
            for (var repeat = 0; repeat < repetitions; repeat++)
                foreach (var corner in source) output.Add(corner with { Point = Rotate(corner.Point, repeat * Math.Tau / repetitions) });
        }
        else
        {
            var section = Math.Tau / (repetitions * 2);
            var firstAngle = Math.Atan2(source[0].Point.Y, source[0].Point.X);
            for (var repeat = 0; repeat < repetitions * 2; repeat++)
            for (var index = 0; index < source.Length; index++)
            {
                var i = repeat % 2 == 0 ? index : source.Length - 1 - index;
                if (i == 0 && repeat % 2 != 0) continue;
                var corner = source[i];
                var angle = Math.Atan2(corner.Point.Y, corner.Point.X);
                var finalAngle = section * repeat + (repeat % 2 == 0 ? angle : section - angle + 2 * firstAngle);
                output.Add(corner with { Point = Unit(finalAngle) * corner.Point.Length });
            }
        }
        return output.ToArray();
    }
    private static Vector[] Polygon(Corner[] corners)
    {
        var n = corners.Length;
        var cuts = new double[n];
        for (var i = 0; i < n; i++)
        {
            var prev = corners[(i + n - 1) % n].Point - corners[i].Point;
            var next = corners[(i + 1) % n].Point - corners[i].Point;
            var cosine = Math.Clamp(Vector.Dot(prev, next) / (prev.Length * next.Length), -.999999, .999999);
            cuts[i] = corners[i].Radius * Math.Sqrt((1 + cosine) / (1 - cosine));
        }
        var limitedCuts = new double[n];
        for (var i = 0; i < n; i++)
        {
            var previous = (i + n - 1) % n;
            var next = (i + 1) % n;
            var before = (corners[previous].Point - corners[i].Point).Length;
            var after = (corners[next].Point - corners[i].Point).Length;
            var scale = Math.Min(1, Math.Min(before / Math.Max(1e-9, cuts[previous] + cuts[i]), after / Math.Max(1e-9, cuts[next] + cuts[i])));
            limitedCuts[i] = cuts[i] * scale;
        }
        var outline = new List<Vector>();
        for (var i = 0; i < n; i++)
        {
            var vertex = corners[i].Point;
            var prev = corners[(i + n - 1) % n].Point - vertex;
            var next = corners[(i + 1) % n].Point - vertex;
            var u = prev / prev.Length;
            var v = next / next.Length;
            var tangent1 = vertex + u * limitedCuts[i];
            var tangent2 = vertex + v * limitedCuts[i];
            var bisector = u + v;
            var cosine = Math.Clamp(Vector.Dot(u, v), -.999999, .999999);
            var radius = limitedCuts[i] * Math.Sqrt((1 - cosine) / (1 + cosine));
            if (radius < 1e-9) { outline.Add(vertex); continue; }
            var center = vertex + bisector / bisector.Length * (radius / Math.Sqrt((1 - cosine) / 2));
            var start = Math.Atan2((tangent1 - center).Y, (tangent1 - center).X);
            var end = Math.Atan2((tangent2 - center).Y, (tangent2 - center).X);
            var sweep = end - start;
            if (sweep > Math.PI) sweep -= Math.Tau;
            if (sweep < -Math.PI) sweep += Math.Tau;
            for (var step = 0; step <= 12; step++) outline.Add(center + Unit(start + sweep * step / 12) * radius);
        }
        var centerOffset = new Vector((outline.Min(p => p.X) + outline.Max(p => p.X)) / 2, (outline.Min(p => p.Y) + outline.Max(p => p.Y)) / 2);
        var maxRadius = outline.Max(p => (p - centerOffset).Length);
        var normalized = outline.Select(p => (p - centerOffset) / maxRadius).ToArray();
        var samples = new Vector[Samples];
        static double Cross(Vector a, Vector b) => a.X * b.Y - a.Y * b.X;
        for (var i = 0; i < Samples; i++)
        {
            var ray = Unit(i * Math.Tau / Samples);
            var distance = 0d;
            for (var j = 0; j < normalized.Length; j++)
            {
                var first = normalized[j];
                var edge = normalized[(j + 1) % normalized.Length] - first;
                var denominator = Cross(ray, edge);
                if (Math.Abs(denominator) < 1e-9) continue;
                var along = Cross(first, edge) / denominator;
                var fraction = Cross(first, ray) / denominator;
                if (along >= 0 && fraction is >= 0 and <= 1) distance = Math.Max(distance, along);
            }
            samples[i] = ray * distance;
        }
        return samples;
    }
}
