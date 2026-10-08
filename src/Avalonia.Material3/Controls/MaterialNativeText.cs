using System.Buffers.Binary;
using System.Security.Cryptography;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Avalonia.Material3.Controls;

// Matched clock numerals and stock Latin action text retain public shaping with
// Android's grayscale raster/measurement. Other faces keep their existing routes.
internal sealed class MaterialNativeText : IDisposable
{
    private const string DefaultRoboto = "9CA9DEBB09459BF4E3E7F826F5CD0F35F253902B85684921FCE2BA3F28DD0F50";
    private const string DerivedRoboto = "BC75B0FDA23E7859E81034E2571126341636CD9C8853B66A57D51D17D094433F";
    private const string MediumRoboto = "185159164472F2F4AFD33BEAFB4CC3841C8329BEFA96642C7443E5BD9E5F8AB8";
    private static readonly Dictionary<GlyphTypeface, Face> Faces = [];
    private static readonly HashSet<string> MatrixDiagnostics = [];
    private sealed class Face(GlyphTypeface owner, SKTypeface typeface, bool numericProfile, bool latinProfile)
    {
        internal readonly GlyphTypeface Owner = owner;
        internal readonly SKTypeface Typeface = typeface;
        internal int References;
        internal readonly bool NumericProfile = numericProfile;
        internal readonly bool LatinProfile = latinProfile;
    }
    private sealed class FaceLease(Face face) : IDisposable
    {
        internal Face Face { get; } = face;
        private bool _disposed;
        internal FaceLease Retain()
        { lock (Faces) { Face.References++; return new(Face); } }
        public void Dispose()
        {
            lock (Faces)
            {
                if (_disposed) return; _disposed = true;
                if (--Face.References != 0) return;
                Faces.Remove(Face.Owner); Face.Typeface.Dispose();
            }
        }
    }
    private readonly FaceLease _face;
    private readonly GlyphInfo[] _glyphs;
    private readonly double _size, _baseline, _tracking;
    private MaterialNativeText(FaceLease face, GlyphInfo[] glyphs, double size, double baseline, double tracking)
    { _face = face; _glyphs = glyphs; _size = size; _baseline = baseline; _tracking = tracking; }

    internal static MaterialNativeText? TryCreate(string text, TextLayout layout, double tracking)
    {
        if (text.Length == 0 || text.Any(character => character is < '0' or > '9') || layout.TextLines.Count != 1) return null;
        var runs = layout.TextLines[0].TextRuns.OfType<ShapedTextRun>().ToArray();
        if (runs.Length != 1) return null;
        var run = runs[0].GlyphRun;
        var face = Acquire(run.GlyphTypeface);
        return face is null ? null : new(face, run.GlyphInfos.ToArray(), run.FontRenderingEmSize, layout.TextLines[0].Baseline, tracking);
    }
    internal static MaterialNativeText? TryCreateAction(string text, TextLayout layout, double tracking)
    {
        if (text.Length == 0 || text.Any(character => character is < ' ' or > '~') || layout.TextLines.Count != 1) return null;
        var runs = layout.TextLines[0].TextRuns.OfType<ShapedTextRun>().ToArray();
        if (runs.Length != 1) return null;
        var run = runs[0].GlyphRun;
        var glyphs = run.GlyphInfos.ToArray();
        if (glyphs.Length != text.Length || glyphs.Any(glyph => glyph.GlyphOffset != default)) return null;
        var face = Acquire(run.GlyphTypeface);
        if (face is null) return null;
        if (!face.Face.LatinProfile) { face.Dispose(); return null; }
        return new(face, glyphs, run.FontRenderingEmSize, layout.TextLines[0].Baseline, tracking);
    }
    private static FaceLease? Acquire(GlyphTypeface chosen)
    {
        if (chosen.FontSimulations != FontSimulations.None) return null;
        lock (Faces)
        {
            if (!Faces.TryGetValue(chosen, out var shared))
            {
                if (!chosen.PlatformTypeface.TryGetStream(out var stream)) return null;
                byte[] bytes;
                using (stream) { using var copy = new MemoryStream(); stream.CopyTo(copy); bytes = copy.ToArray(); }
                // Arbitrary active variation coordinates are absent from this API.
                // The paired400 file is explicitly pinned at its default axes.
                var sourceHash = Convert.ToHexString(SHA256.HashData(bytes));
                var latinProfile = sourceHash == MediumRoboto && (int)chosen.Weight == 500 && (int)chosen.Stretch == 5 && chosen.Style == FontStyle.Normal;
                var fvar = Tag("fvar");
                var derived = chosen.PlatformTypeface.TryGetTable(new OpenTypeTag(fvar), out _);
                if (derived)
                {
                    if (sourceHash != DefaultRoboto || (int)chosen.Weight != 400 || (int)chosen.Stretch != 5 || chosen.Style != FontStyle.Normal) return null;
                    using var reproduction = AssetLoader.Open(new Uri("avares://Avalonia.Material3/Assets/Fonts/Roboto-Clock400.ttf"));
                    using var copy = new MemoryStream(); reproduction.CopyTo(copy); bytes = copy.ToArray();
                    if (Convert.ToHexString(SHA256.HashData(bytes)) != DerivedRoboto) return null;
                }
                using var data = SKData.CreateCopy(bytes);
                var count = bytes.Length >= 12 && BinaryPrimitives.ReadUInt32BigEndian(bytes) == Tag("ttcf")
                    ? BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(8)) : 1;
                if (count > 256) return null;
                SKTypeface? imported = null;
                for (var index = 0; index < count; index++)
                {
                    var candidate = SKTypeface.FromData(data, index);
                    if (candidate is null) continue;
                    if (Corresponds(candidate, chosen, derived)) { imported = candidate; break; }
                    candidate.Dispose();
                }
                if (imported is null) return null;
                shared = new(chosen, imported, derived, latinProfile); Faces.Add(chosen, shared);
            }
            shared.References++; return new(shared);
        }
    }
    private static uint Tag(string name) => (uint)name[0] << 24 | (uint)name[1] << 16 | (uint)name[2] << 8 | name[3];
    private static bool Corresponds(SKTypeface imported, GlyphTypeface chosen, bool derived)
    {
        if (imported.GlyphCount != chosen.GlyphCount || imported.FontStyle.Weight != (int)chosen.Weight ||
            imported.FontStyle.Width != (int)chosen.Stretch ||
            (imported.FontStyle.Slant == SKFontStyleSlant.Upright) != (chosen.Style == FontStyle.Normal)) return false;
        // The explicitly hashed derivation changes variation outlines/metadata;
        // its exact source/default axes and unchanged cmap/glyph order are recorded
        // by the offline manifest. Ordinary TTC imports require raw table identity.
        foreach (var table in derived ? new[] { "cmap" } : new[] { "head", "maxp", "cmap", "name", "OS/2", "glyf", "CFF " })
        {
            var tag = Tag(table);
            if (!chosen.PlatformTypeface.TryGetTable(new OpenTypeTag(tag), out var actual)) continue;
            if (imported.GetTableData(tag) is not { } source || !actual.Span.SequenceEqual(source)) return false;
        }
        return true;
    }
    internal bool Draw(DrawingContext context, IBrush? brush, Point origin, double density, Size bounds, TextOptions options)
    {
        if (!CanPaint(brush, options) || brush is not ISolidColorBrush solid) return false;
        context.Custom(new GlyphDraw(_face.Retain(), _glyphs, _size, _baseline, _tracking, solid.Color, solid.Opacity, origin, density, bounds, options));
        return true;
    }
    private static bool CanPaint(IBrush? brush, TextOptions options) => brush is ISolidColorBrush
        && options.TextHintingMode is not (TextHintingMode.Light or TextHintingMode.None)
        && options.TextRenderingMode is TextRenderingMode.Unspecified or TextRenderingMode.Antialias;
    private static double[] NativeAdvances(Face face, GlyphInfo[] glyphs, double size, double tracking, double density)
    {
        var shapeSize = Math.Floor(size * density);
        var nativeTracking = tracking / size * shapeSize;
        var advances = new double[glyphs.Length];
        for (var index = 0; index < glyphs.Length; index++)
        {
            face.Owner.TryGetHorizontalGlyphAdvance(glyphs[index].GlyphIndex, out var units);
            var designAdvance = (double)units / face.Owner.Metrics.DesignEmHeight;
            var hinted = Math.Floor(designAdvance * shapeSize + .5);
            // Preserve HarfBuzz GPOS deltas after replacing the advance callback.
            // HB's scaled positioning stays in its physical26.8 integer grid.
            var adjustment = face.LatinProfile
                ? Math.Floor(((glyphs[index].GlyphAdvance - tracking) / size - designAdvance) * shapeSize * 256 + .5) / 256 : 0;
            advances[index] = hinted + adjustment + nativeTracking;
        }
        return advances;
    }
    internal Size? Measure(double density, double height, IBrush? brush, TextOptions options)
    {
        if (!(_face.Face.NumericProfile || _face.Face.LatinProfile) || !CanPaint(brush, options)) return null;
        // Locked numeric/Latin profiles have no instructions and phantom pp1=0.
        // FreeType rounds pp2 even without bytecode. TextLine LEFT/RIGHT trims
        // edge half-tracking; Compose reserves.5 after the first intrinsic ceil.
        var shapeSize = Math.Floor(_size * density);
        var tracking = _tracking / _size * shapeSize;
        var advance = NativeAdvances(_face.Face, _glyphs, _size, _tracking, density).Sum() - tracking;
        return new Size(Math.Ceiling(Math.Ceiling(advance) + (_tracking == 0 ? 0 : .5)) / density, Math.Ceiling(height * density) / density);
    }
    public void Dispose() => _face.Dispose();

    private sealed class GlyphDraw(FaceLease face, GlyphInfo[] glyphs, double size, double baseline, double tracking, Color color,
        double alpha, Point origin, double density, Size bounds, TextOptions options) : ICustomDrawOperation
    {
        public Rect Bounds => new(bounds);
        public bool HitTest(Point point) => false;
        public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);
        public void Dispose() => face.Dispose();
        public void Render(ImmediateDrawingContext context)
        {
            if (context.TryGetFeature<ISkiaSharpApiLeaseFeature>() is { } feature)
            {
                using var lease = feature.Lease(); Paint(lease.SkCanvas, lease.CurrentOpacity); return;
            }
            var pixels = new PixelSize(Math.Max(1, (int)Math.Ceiling(bounds.Width * density)), Math.Max(1, (int)Math.Ceiling(bounds.Height * density)));
            using var bitmap = new Avalonia.Media.Imaging.WriteableBitmap(pixels, new Vector(96 * density, 96 * density), PixelFormat.Bgra8888, AlphaFormat.Premul);
            using (var storage = bitmap.Lock())
            using (var surface = SKSurface.Create(new SKImageInfo(pixels.Width, pixels.Height, SKColorType.Bgra8888, SKAlphaType.Premul), storage.Address, storage.RowBytes))
            { surface.Canvas.Clear(SKColors.Transparent); surface.Canvas.Scale((float)density); Paint(surface.Canvas, 1); }
            context.DrawBitmap(bitmap, new Rect(bounds));
        }
        private void Paint(SKCanvas canvas, double opacity)
        {
            using var font = new SKFont(face.Face.Typeface, (float)(size * density))
            {
                Edging = SKFontEdging.Antialias, Subpixel = false, LinearMetrics = false, EmbeddedBitmaps = true,
                BaselineSnap = options.BaselinePixelAlignment != BaselinePixelAlignment.Unaligned,
                Hinting = options.TextHintingMode == TextHintingMode.None ? SKFontHinting.None : SKFontHinting.Normal
            };
            using var paint = new SKPaint { IsAntialias = true, Color = new SKColor(color.R, color.G, color.B,
                (byte)Math.Clamp(Math.Round(color.A * alpha * opacity), 0, 255)) };
            var indices = new ushort[glyphs.Length]; var positions = new SKPoint[glyphs.Length]; double x = 0;
            var nativeProfile = face.Face.NumericProfile || face.Face.LatinProfile;
            var nativeAdvances = nativeProfile ? NativeAdvances(face.Face, glyphs, size, tracking, density) : null;
            for (var index = 0; index < glyphs.Length; index++)
            {
                var glyph = glyphs[index]; indices[index] = glyph.GlyphIndex;
                positions[index] = new((float)((x + glyph.GlyphOffset.X) * density), (float)(glyph.GlyphOffset.Y * density));
                x += nativeAdvances is { } advances ? advances[index] / density : glyph.GlyphAdvance;
            }
            var saved = canvas.Save();
            try
            {
                var before = canvas.TotalMatrix;
                canvas.Scale((float)(1 / density));
                using var builder = new SKTextBlobBuilder();
                var run = builder.AllocatePositionedRun(font, indices.Length);
                run.SetGlyphs(indices); run.SetPositions(positions);
                using var blob = builder.Build();
                var paragraphBaseline = nativeProfile && options.BaselinePixelAlignment != BaselinePixelAlignment.Unaligned
                    ? Math.Floor(baseline * density + .5) : baseline * density;
                if (face.Face.NumericProfile && indices.Length == 1 && indices[0] == 24)
                {
                    var after = canvas.TotalMatrix;
                    var widths = font.GetGlyphWidths(indices);
                    font.MeasureText(indices, out var ink);
                    var metrics = font.Metrics;
                    var diagnostic = $"M3FontMatrix density={density:R} size={font.Size:R} glyph={indices[0]} offset={glyphs[0].GlyphOffset} position={positions[0].X:R},{positions[0].Y:R} origin={origin.X:R},{origin.Y:R} baseline={paragraphBaseline:R} " +
                        $"before={before.ScaleX:R},{before.ScaleY:R},{before.SkewX:R},{before.SkewY:R},{before.TransX:R},{before.TransY:R} after={after.ScaleX:R},{after.ScaleY:R},{after.SkewX:R},{after.SkewY:R},{after.TransX:R},{after.TransY:R} " +
                        $"width={widths[0]:R} bounds={ink.Left:R},{ink.Top:R},{ink.Right:R},{ink.Bottom:R} metrics={metrics.Top:R},{metrics.Ascent:R},{metrics.Descent:R},{metrics.Bottom:R} hint={font.Hinting} subpixel={font.Subpixel} linear={font.LinearMetrics} bitmap={font.EmbeddedBitmaps} autoHint={font.ForceAutoHinting} opacity={opacity:R}";
                    lock (MatrixDiagnostics)
                        if (MatrixDiagnostics.Count < 24 && MatrixDiagnostics.Add(diagnostic)) Console.WriteLine(diagnostic);
                }
                canvas.DrawText(blob, (float)(origin.X * density), (float)(origin.Y * density + paragraphBaseline), paint);
            }
            finally { canvas.RestoreToCount(saved); }
        }
    }
}
