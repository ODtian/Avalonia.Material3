// Copyright 2016 Google Inc. BSD-3-Clause; see Assets/Shadow/LICENSE.txt.
// Source: platform/external/skia android-15.0.0_r1 ShadowRRectOp.cpp / GrShadowGeoProc.cpp.
using Avalonia.Media;
using SkiaSharp;

namespace Avalonia.Material3.Controls;

internal static class MaterialShadowMesh
{
    private static readonly Lazy<SKRuntimeEffect> Effect = new(() => SKRuntimeEffect.CreateShader("""
        uniform shader falloff;
        uniform float correction;
        uniform float alpha;
        uniform float mode;
        half4 main(float2 p) {
            half2 offset = mode == 0.0 ? half2(p) : mode == 1.0 ? half2(0.0, p.y) : half2(0.0);
            half d = length(offset);
            float t = correction * (1.0 - d);
            half coverage = falloff.eval(float2(t * 128.0, 0.5)).a;
            return half4(0.0, 0.0, 0.0, coverage * alpha);
        }
        """, out var error) ?? throw new InvalidOperationException(error));
    private static readonly Lazy<SKBitmap> Falloff = new(() =>
    {
        var bitmap = new SKBitmap(new SKImageInfo(128, 1, SKColorType.Alpha8, SKAlphaType.Premul));
        for (var i = 0; i < 128; i++)
        {
            var distance = 1 - i / 127d;
            var alpha = (byte)Math.Round((Math.Exp(-4 * distance * distance) - .018) * 255, MidpointRounding.AwayFromZero);
            System.Runtime.InteropServices.Marshal.WriteByte(bitmap.GetPixels(), i, alpha);
        }
        return bitmap;
    });
    private static readonly ushort[] Indices = [
        0,6,25,0,25,24, 6,18,27,6,27,25, 18,12,26,18,26,27, 12,0,24,12,24,26,
        0,1,2,0,2,3,0,3,4,0,4,5, 6,11,10,6,10,9,6,9,8,6,8,7,
        12,17,16,12,16,15,12,15,14,12,14,13, 18,19,20,18,20,21,18,21,22,18,22,23,
        0,5,11,0,11,6, 6,7,19,6,19,18, 18,23,17,18,17,12, 12,13,1,12,1,0,
        0,6,18,0,18,12 ];

    internal static void Draw(SKCanvas canvas, Rect bounds, double radius, double blur, double inset, byte alpha, double casterAlpha, double inheritedOpacity)
    {
        if (blur <= 0 || bounds.Width <= 0 || bounds.Height <= 0) return;
        var half = Math.Min(bounds.Width, bounds.Height) / 2;
        radius = Math.Min(radius, half);
        var fill = inset > half || casterAlpha < 1;
        var umbra = Math.Min(Math.Max(radius, blur), half);
        var inner = fill ? 0 : Math.Max(inset - Math.Max(radius, blur), 0);
        var correction = umbra / blur;
        var vector = new Vector(radius - umbra, -radius - umbra);
        vector /= vector.Length;
        var diagonal = umbra / (Math.Sqrt(2) * (radius - umbra) - radius);
        var positions = new SKPoint[inner > 0 ? 28 : 24];
        var offsets = new SKPoint[positions.Length];
        for (var i = 0; i < 4; i++)
        {
            var right = i % 2 == 1; var bottom = i >= 2;
            var outerX = right ? bounds.Right : bounds.Left; var outerY = bottom ? bounds.Bottom : bounds.Top;
            var midX = outerX + (right ? -radius : radius); var midY = outerY + (bottom ? -radius : radius);
            var innerX = outerX + (right ? -umbra : umbra); var innerY = outerY + (bottom ? -umbra : umbra);
            var start = i * 6;
            positions[start] = new((float)innerX, (float)innerY); offsets[start] = new(0, 0);
            positions[start + 1] = new((float)outerX, (float)innerY); offsets[start + 1] = new(0, -1);
            positions[start + 2] = new((float)outerX, (float)midY); offsets[start + 2] = new((float)vector.X, (float)vector.Y);
            positions[start + 3] = new((float)outerX, (float)outerY); offsets[start + 3] = new((float)diagonal, (float)diagonal);
            positions[start + 4] = new((float)midX, (float)outerY); offsets[start + 4] = new((float)vector.X, (float)vector.Y);
            positions[start + 5] = new((float)innerX, (float)outerY); offsets[start + 5] = new(0, -1);
        }
        if (inner > 0)
        {
            var distance = umbra + inner;
            positions[24] = new((float)(bounds.Left + distance), (float)(bounds.Top + distance));
            positions[25] = new((float)(bounds.Right - distance), (float)(bounds.Top + distance));
            positions[26] = new((float)(bounds.Left + distance), (float)(bounds.Bottom - distance));
            positions[27] = new((float)(bounds.Right - distance), (float)(bounds.Bottom - distance));
        }
        using var image = SKImage.FromBitmap(Falloff.Value);
        using var table = image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, new SKSamplingOptions(SKFilterMode.Linear));
        void DrawGroup(ushort[] selectedIndices, int mode, bool horizontal = false)
        {
            var points = selectedIndices.Select(i => positions[i]).ToArray();
            // SkVertices texture interpolation requires a non-degenerate texture
            // triangle. Native edge varyings are collinear, so encode a tangent
            // coordinate and ignore it in the edge shader; source coverage is unchanged.
            var texture = mode == 0 ? selectedIndices.Select(i => offsets[i]).ToArray()
                : mode == 1 ? selectedIndices.Select(i => new SKPoint(horizontal ? positions[i].X : positions[i].Y, offsets[i].Y)).ToArray() : null;
            using var vertices = SKVertices.CreateCopy(SKVertexMode.Triangles, points, texture, Enumerable.Repeat(SKColors.White, points.Length).ToArray());
            var uniforms = new SKRuntimeEffectUniforms(Effect.Value) { ["correction"] = (float)correction,
                ["alpha"] = (float)((byte)(alpha * casterAlpha) / 255d * inheritedOpacity), ["mode"] = (float)mode };
            using var shader = Effect.Value.ToShader(uniforms, new SKRuntimeEffectChildren(Effect.Value) { ["falloff"] = table });
            using var paint = new SKPaint { Shader = shader, IsAntialias = false, Color = SKColors.White };
            canvas.DrawVertices(vertices, SKBlendMode.Src, paint);
        }
        DrawGroup(Indices[24..72], 0);
        DrawGroup([.. Indices[72..78], .. Indices[84..90]], 1, true);
        DrawGroup([.. Indices[78..84], .. Indices[90..96]], 1);
        if (inner > 0) DrawGroup(Indices[..24], 2);
        if (fill) DrawGroup(Indices[96..], 2);
    }
}
