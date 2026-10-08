// Copyright 2017 Google Inc. BSD-3-Clause; see Assets/Shadow/LICENSE.txt.
// Source: Skia android-15.0.0_r1 SkShadowTessellator.cpp / SkPolyUtils.cpp.
using Avalonia.Media;
using SkiaSharp;

namespace Avalonia.Material3.Controls;

// Convex source subset for nonuniform rounded rectangles and affine caster transforms.
internal static class MaterialConvexShadow
{
    private static readonly Lazy<SKRuntimeEffect> Gaussian = new(() => SKRuntimeEffect.CreateColorFilter("""
        uniform float alpha;
        half4 main(half4 color) {
            half d = 1.0 - color.a;
            half g = exp(-4.0 * d * d) - 0.018;
            return half4(0.0, 0.0, 0.0, g * alpha);
        }
        """, out var error) ?? throw new InvalidOperationException(error));

    internal static void Draw(SKCanvas canvas, RoundedRect shape, MaterialNativeShadow.Lighting light, double elevation, double opacity)
    {
        using var rounded = new SKRoundRect();
        var rect = shape.Rect;
        rounded.SetRectRadii(new SKRect((float)rect.Left, (float)rect.Top, (float)rect.Right, (float)rect.Bottom),
            [new((float)shape.RadiiTopLeft.X, (float)shape.RadiiTopLeft.Y), new((float)shape.RadiiTopRight.X, (float)shape.RadiiTopRight.Y),
             new((float)shape.RadiiBottomRight.X, (float)shape.RadiiBottomRight.Y), new((float)shape.RadiiBottomLeft.X, (float)shape.RadiiBottomLeft.Y)]);
        using var path = new SKPath(); path.AddRoundRect(rounded);
        var original = Flatten(path, light.ToDevice);
        var z = elevation * light.Density;
        var ambient = (float)Math.Min(z / 2, 150);
        var ratio = Math.Clamp(z / (light.Height - z), 0, .95);
        var scale = Math.Clamp(light.Height / (light.Height - z), 1, 1.95);
        var projected = Flatten(path, light.ToDevice * Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(-ratio * light.Light.X, -ratio * light.Light.Y));
        var spot = (float)(light.Radius * ratio);
        var inherited = light.CasterAlpha > 0 ? opacity / light.CasterAlpha : 0;
        Mesh(canvas, original, (float)(ambient * z / 128), ambient, null, 9, light.CasterAlpha, inherited);
        Mesh(canvas, projected, spot, spot, original, 48, light.CasterAlpha, inherited);
    }

    private static SKPoint[] Flatten(SKPath path, Matrix transform)
    {
        var polygon = new List<SKPoint>();
        SKPoint Map(SKPoint point)
        { var mapped = new Point(point.X, point.Y).Transform(transform); return new((float)mapped.X, (float)mapped.Y); }
        void Add(SKPoint point)
        {
            point = new(MathF.Floor(point.X * 16 + .5f) / 16, MathF.Floor(point.Y * 16 + .5f) / 16);
            if (polygon.Count > 0 && Distance(point, polygon[^1]) < .0625f) return;
            if (polygon.Count > 1 && Math.Abs(Cross(polygon[^1] - polygon[^2], point - polygon[^1])) < 1f / 4096) polygon.RemoveAt(polygon.Count - 1);
            polygon.Add(point);
        }
        void Quad(SKPoint a, SKPoint b, SKPoint c, int level = 0)
        {
            var chord = c - a;
            var distance = Math.Abs(Cross(b - a, chord)) / Math.Max(.0001f, Length(chord));
            if (distance <= .2f || level >= 10) { Add(c); return; }
            var ab = Scale(a + b, .5f); var bc = Scale(b + c, .5f); var middle = Scale(ab + bc, .5f);
            Quad(a, ab, middle, level + 1); Quad(middle, bc, c, level + 1);
        }
        using var iterator = path.CreateRawIterator(); var points = new SKPoint[4];
        for (var verb = iterator.Next(points); verb != SKPathVerb.Done; verb = iterator.Next(points))
        {
            if (verb == SKPathVerb.Move) Add(Map(points[0]));
            else if (verb == SKPathVerb.Line) Add(Map(points[1]));
            else if (verb == SKPathVerb.Quad) Quad(Map(points[0]), Map(points[1]), Map(points[2]));
            else if (verb == SKPathVerb.Conic)
            {
                var a = Map(points[0]); var b = Map(points[1]); var c = Map(points[2]); var weight = iterator.ConicWeight();
                var factor = (weight - 1) / (4 * (1 + weight));
                var error = Length(Scale(a - Scale(b, 2) + c, factor)); var power = 0;
                while (power < 5 && error > .25f) { error *= .25f; power++; }
                var quads = SKPath.ConvertConicToQuads(a, b, c, weight, power);
                for (var i = 0; i < quads.Length - 2; i += 2) Quad(quads[i], quads[i + 1], i == quads.Length - 3 ? c : quads[i + 2]);
            }
        }
        if (polygon.Count > 1 && Distance(polygon[0], polygon[^1]) < .0625f) polygon.RemoveAt(polygon.Count - 1);
        if (polygon.Count > 2 && Math.Abs(Cross(polygon[0] - polygon[^1], polygon[1] - polygon[0])) < 1f / 4096)
        { polygon[0] = polygon[^1]; polygon.RemoveAt(polygon.Count - 1); }
        return polygon.ToArray();
    }

    private static void Mesh(SKCanvas canvas, SKPoint[] polygon, float inset, float outset, SKPoint[]? clip, byte alpha, double casterAlpha, double inheritedOpacity)
    {
        if (polygon.Length < 3 || outset <= 0) return;
        var area = 0f; var centroid = new SKPoint(); var first = polygon[0];
        for (var i = 1; i < polygon.Length - 1; i++)
        {
            var a = polygon[i] - first; var b = polygon[i + 1] - first; var cross = Cross(a, b);
            centroid += Scale(a + b, cross); area += cross;
        }
        if (Math.Abs(area) < .0001f) return;
        centroid = Scale(centroid, 1 / (3 * area)) + first;
        var winding = area > 0 ? 1 : -1;
        var minDistance = float.PositiveInfinity;
        for (var i = 0; i < polygon.Length; i++)
        {
            var a = polygon[i]; var b = polygon[(i + 1) % polygon.Length]; var edge = b - a;
            var point = a + Scale(edge, Math.Clamp(Dot(centroid - a, edge) / Dot(edge, edge), 0, 1));
            minDistance = Math.Min(minDistance, Distance(centroid, point));
        }
        byte innerAlpha = 255;
        if (inset > 0 && minDistance < inset + .01f)
        {
            var nextInset = Math.Max(0, minDistance - .01f);
            var ratio = (uint)(128 * (nextInset / inset + 1));
            innerAlpha = (byte)((255 * (256 - ratio)) >> 8); inset = nextInset;
        }
        var innerPolygon = inset > .0001f ? Inset(polygon, inset, winding) : polygon;
        var validInner = innerPolygon.Length >= 3;
        SKPoint Inner(SKPoint point) => validInner ? innerPolygon.MinBy(p => Dot(p - point, p - point)) : point + Scale(centroid - point, .95f);
        var transparent = casterAlpha < 1 || clip is not null && !Inside(centroid, clip);
        var positions = new List<SKPoint>(); var colors = new List<SKColor>(); var indices = new List<ushort>();
        int Add(SKPoint point, byte coverage) { positions.Add(point); colors.Add(new SKColor(0, 0, 0, coverage)); return positions.Count - 1; }
        void Triangle(int a, int b, int c) { indices.Add((ushort)a); indices.Add((ushort)b); indices.Add((ushort)c); }
        var center = transparent ? Add(centroid, innerAlpha) : -1;
        var previous = polygon[^1];
        SKPoint Normal(SKPoint a, SKPoint b) { var edge = b - a; return Scale(new SKPoint(edge.Y, -edge.X), winding * outset / Length(edge)); }
        var firstNormal = Normal(previous, polygon[0]);
        var firstInner = Add(Inner(previous), innerAlpha); var previousInner = firstInner;
        var firstOuter = Add(previous + firstNormal, 0); var previousOuter = firstOuter;
        var previousNormal = firstNormal;
        for (var i = 0; i < polygon.Length; i++)
        {
            var point = polygon[i]; var normal = i == 0 ? firstNormal : Normal(previous, point);
            var angle = MathF.Atan2(Cross(previousNormal, normal), Dot(previousNormal, normal));
            var steps = (int)MathF.Floor(Math.Abs(outset * angle * .25f) + .5f);
            var increment = steps > 0 ? angle / steps : 0;
            var rotated = previousNormal;
            for (var step = 1; step <= steps; step++)
            {
                rotated = new(rotated.X * MathF.Cos(increment) - rotated.Y * MathF.Sin(increment), rotated.Y * MathF.Cos(increment) + rotated.X * MathF.Sin(increment));
                var arc = Add(previous + (step == steps ? normal : rotated), 0); Triangle(previousInner, arc, previousOuter); previousOuter = arc;
            }
            var inner = i == polygon.Length - 1 ? firstInner : Add(Inner(point), innerAlpha);
            var outer = Add(point + normal, 0);
            Triangle(previousInner, previousOuter, inner); Triangle(previousOuter, outer, inner);
            if (transparent) Triangle(center, previousInner, inner);
            else if (clip is not null)
            {
                var before = ClipToCaster(positions[previousInner], centroid, clip);
                var after = ClipToCaster(positions[inner], centroid, clip);
                if (before is { } left && after is { } right)
                { var leftIndex = Add(left, innerAlpha); var rightIndex = Add(right, innerAlpha); Triangle(previousInner, inner, rightIndex); Triangle(previousInner, rightIndex, leftIndex); }
                else if (after is { } rightOnly) Triangle(previousInner, inner, Add(rightOnly, innerAlpha));
                else if (before is { } leftOnly) Triangle(previousInner, inner, Add(leftOnly, innerAlpha));
            }
            previous = point; previousInner = inner; previousOuter = outer; previousNormal = normal;
        }
        var finalAngle = MathF.Atan2(Cross(previousNormal, firstNormal), Dot(previousNormal, firstNormal));
        var finalSteps = (int)MathF.Floor(Math.Abs(outset * finalAngle * .25f) + .5f);
        var finalRotated = previousNormal;
        for (var step = 1; step < finalSteps; step++)
        {
            var increment = finalAngle / finalSteps;
            finalRotated = new(finalRotated.X * MathF.Cos(increment) - finalRotated.Y * MathF.Sin(increment), finalRotated.Y * MathF.Cos(increment) + finalRotated.X * MathF.Sin(increment));
            var arc = Add(previous + finalRotated, 0); Triangle(previousInner, arc, previousOuter); previousOuter = arc;
        }
        Triangle(firstInner, previousOuter, firstOuter);
        using var vertices = SKVertices.CreateCopy(SKVertexMode.Triangles, positions.ToArray(), null, colors.ToArray(), indices.ToArray());
        var uniforms = new SKRuntimeEffectUniforms(Gaussian.Value) { ["alpha"] = (float)((byte)(alpha * casterAlpha) / 255d * inheritedOpacity) };
        using var filter = Gaussian.Value.ToColorFilter(uniforms);
        using var paint = new SKPaint { Color = SKColors.Black, ColorFilter = filter, IsAntialias = false };
        canvas.DrawVertices(vertices, SKBlendMode.Dst, paint);
    }

    private static SKPoint[] Inset(SKPoint[] polygon, float distance, int winding)
    {
        var result = polygon.ToList();
        for (var i = 0; i < polygon.Length && result.Count >= 3; i++)
        {
            var a = polygon[i]; var edge = polygon[(i + 1) % polygon.Length] - a;
            var threshold = distance * Length(edge); var input = result; result = [];
            var previous = input[^1]; var before = Cross(edge, previous - a) * winding - threshold;
            foreach (var point in input)
            {
                var after = Cross(edge, point - a) * winding - threshold;
                if (after * before < 0) result.Add(previous + Scale(point - previous, before / (before - after)));
                if (after >= 0) result.Add(point);
                previous = point; before = after;
            }
        }
        return result.Where((point, index) => index == 0 || Distance(point, result[index - 1]) > .01f).ToArray();
    }
    private static bool Inside(SKPoint point, SKPoint[] polygon)
    { var sign = 0f; for (var i = 0; i < polygon.Length; i++) { var cross = Cross(polygon[(i + 1) % polygon.Length] - polygon[i], point - polygon[i]); if (cross * sign < 0) return false; if (cross != 0) sign = cross; } return true; }
    private static SKPoint? ClipToCaster(SKPoint point, SKPoint centroid, SKPoint[] polygon)
    {
        var segment = centroid - point;
        for (var i = 0; i < polygon.Length; i++)
        {
            var edge = polygon[(i + 1) % polygon.Length] - polygon[i]; var delta = point - polygon[i];
            var denominator = Cross(edge, segment); if (Math.Abs(denominator) < .0001f) continue;
            var t = Cross(delta, segment) / denominator; var s = Cross(delta, edge) / denominator;
            if (t is >= 0 and <= 1 && s is >= 0 and <= 1) return point + Scale(segment, s);
        }
        return null;
    }
    private static float Length(SKPoint vector) => MathF.Sqrt(Dot(vector, vector));
    private static float Distance(SKPoint a, SKPoint b) => Length(a - b);
    private static float Dot(SKPoint a, SKPoint b) => a.X * b.X + a.Y * b.Y;
    private static float Cross(SKPoint a, SKPoint b) => a.X * b.Y - a.Y * b.X;
    private static SKPoint Scale(SKPoint point, float scale) => new(point.X * scale, point.Y * scale);
}
