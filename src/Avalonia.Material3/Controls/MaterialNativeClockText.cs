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

// Clock numerals retain public shaping/placement, with Android's grayscale
// SkFont raster flags. Import only a demonstrably corresponding public face.
internal sealed class MaterialNativeClockText : IDisposable
{
    // Temporary bounded diagnostic for the exact paired Android profile.
    // Removed after branch/renderer engagement is established by device capture.
    private static readonly HashSet<string> Diagnostics = [];
    private static void Diagnose(string value)
    { lock (Diagnostics) { if (Diagnostics.Count < 160 && Diagnostics.Add(value)) Console.WriteLine(value); } }
    private const string DefaultRoboto = "9CA9DEBB09459BF4E3E7F826F5CD0F35F253902B85684921FCE2BA3F28DD0F50";
    private const string DerivedRoboto = "BC75B0FDA23E7859E81034E2571126341636CD9C8853B66A57D51D17D094433F";
    private static readonly Dictionary<GlyphTypeface, Face> Faces = [];
    private sealed class Face(GlyphTypeface owner, SKTypeface typeface)
    {
        internal readonly GlyphTypeface Owner = owner;
        internal readonly SKTypeface Typeface = typeface;
        internal int References;
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
    private readonly double _size, _baseline;
    private MaterialNativeClockText(FaceLease face, GlyphInfo[] glyphs, double size, double baseline)
    { _face = face; _glyphs = glyphs; _size = size; _baseline = baseline; }

    internal static MaterialNativeClockText? TryCreate(string text, TextLayout layout)
    {
        Diagnose($"M3FONT create text={text} lines={layout.TextLines.Count} runs={string.Join(',', layout.TextLines.SelectMany(line => line.TextRuns).Select(run => run.GetType().Name))}");
        if (text.Length == 0 || text.Any(character => character is < '0' or > '9') || layout.TextLines.Count != 1) { Diagnose("M3FONT reject text/lines"); return null; }
        var runs = layout.TextLines[0].TextRuns.OfType<ShapedTextRun>().ToArray();
        if (runs.Length != 1) { Diagnose("M3FONT reject shaped-count=" + runs.Length); return null; }
        var run = runs[0].GlyphRun;
        var face = Acquire(run.GlyphTypeface);
        Diagnose($"M3FONT create-result text={text} native={face is not null}");
        return face is null ? null : new(face, run.GlyphInfos.ToArray(), run.FontRenderingEmSize, layout.TextLines[0].Baseline);
    }
    private static FaceLease? Acquire(GlyphTypeface chosen)
    {
        Diagnose($"M3FONT acquire chosen={chosen.FamilyName}/{chosen.Weight}/{chosen.Stretch}/{chosen.Style}/{chosen.FontSimulations} count={chosen.GlyphCount}");
        if (chosen.FontSimulations != FontSimulations.None) { Diagnose("M3FONT reject simulations"); return null; }
        lock (Faces)
        {
            if (!Faces.TryGetValue(chosen, out var shared))
            {
                if (!chosen.PlatformTypeface.TryGetStream(out var stream)) { Diagnose("M3FONT reject stream"); return null; }
                byte[] bytes;
                using (stream) { using var copy = new MemoryStream(); stream.CopyTo(copy); bytes = copy.ToArray(); }
                // Arbitrary active variation coordinates are absent from this API.
                // The paired400 file is explicitly pinned at its default axes.
                var fvar = Tag("fvar");
                var derived = chosen.PlatformTypeface.TryGetTable(new OpenTypeTag(fvar), out _);
                Diagnose($"M3FONT fvar={derived} tag={new OpenTypeTag(fvar)} sourceSHA={Convert.ToHexString(SHA256.HashData(bytes))}");
                if (derived)
                {
                    if (Convert.ToHexString(SHA256.HashData(bytes)) != DefaultRoboto || (int)chosen.Weight != 400 || (int)chosen.Stretch != 5 || chosen.Style != FontStyle.Normal) { Diagnose("M3FONT reject derived-profile"); return null; }
                    using var reproduction = AssetLoader.Open(new Uri("avares://Avalonia.Material3/Assets/Fonts/Roboto-Clock400.ttf"));
                    using var copy = new MemoryStream(); reproduction.CopyTo(copy); bytes = copy.ToArray();
                    Diagnose($"M3FONT derivedSHA={Convert.ToHexString(SHA256.HashData(bytes))}");
                    if (Convert.ToHexString(SHA256.HashData(bytes)) != DerivedRoboto) { Diagnose("M3FONT reject derived-sha"); return null; }
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
                    Diagnose($"M3FONT candidate index={index} family={candidate.FamilyName} weight={candidate.FontStyle.Weight} width={candidate.FontStyle.Width} slant={candidate.FontStyle.Slant} count={candidate.GlyphCount}");
                    if (Corresponds(candidate, chosen, derived)) { imported = candidate; break; }
                    candidate.Dispose();
                }
                if (imported is null) { Diagnose("M3FONT reject no-corresponding-face"); return null; }
                shared = new(chosen, imported); Faces.Add(chosen, shared);
            }
            shared.References++; return new(shared);
        }
    }
    private static uint Tag(string name) => (uint)name[0] << 24 | (uint)name[1] << 16 | (uint)name[2] << 8 | name[3];
    private static bool Corresponds(SKTypeface imported, GlyphTypeface chosen, bool derived)
    {
        if (imported.GlyphCount != chosen.GlyphCount || imported.FontStyle.Weight != (int)chosen.Weight ||
            imported.FontStyle.Width != (int)chosen.Stretch ||
            (imported.FontStyle.Slant == SKFontStyleSlant.Upright) != (chosen.Style == FontStyle.Normal)) { Diagnose("M3FONT reject descriptor"); return false; }
        // The explicitly hashed derivation changes variation outlines/metadata;
        // its exact source/default axes and unchanged cmap/glyph order are recorded
        // by the offline manifest. Ordinary TTC imports require raw table identity.
        foreach (var table in derived ? new[] { "cmap" } : new[] { "head", "maxp", "cmap", "name", "OS/2", "glyf", "CFF " })
        {
            var tag = Tag(table);
            if (!chosen.PlatformTypeface.TryGetTable(new OpenTypeTag(tag), out var actual)) continue;
            if (imported.GetTableData(tag) is not { } source || !actual.Span.SequenceEqual(source)) { Diagnose($"M3FONT reject table={table} actualLength={actual.Length}"); return false; }
        }
        return true;
    }
    internal bool Draw(DrawingContext context, IBrush? brush, Point origin, double density, Size bounds, TextOptions options)
    {
        if (brush is not ISolidColorBrush solid || options.TextHintingMode == TextHintingMode.Light) { Diagnose($"M3FONT reject draw brush={brush?.GetType().Name} hint={options.TextHintingMode}"); return false; }
        Diagnose($"M3FONT draw accepted size={_size} density={density} origin={origin} baseline={_baseline} options={options}");
        context.Custom(new GlyphDraw(_face.Retain(), _glyphs, _size, _baseline, solid.Color, solid.Opacity, origin, density, bounds, options));
        return true;
    }
    public void Dispose() => _face.Dispose();

    private sealed class GlyphDraw(FaceLease face, GlyphInfo[] glyphs, double size, double baseline, Color color,
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
            Diagnose($"M3FONT paint fontSize={size * density} subpixel=false linear=false baselineSnap={options.BaselinePixelAlignment != BaselinePixelAlignment.Unaligned} hint={options.TextHintingMode} opacity={opacity} matrix={canvas.TotalMatrix}");
            using var font = new SKFont(face.Face.Typeface, (float)(size * density))
            {
                Edging = SKFontEdging.Antialias, Subpixel = false, LinearMetrics = false,
                BaselineSnap = options.BaselinePixelAlignment != BaselinePixelAlignment.Unaligned,
                Hinting = options.TextHintingMode == TextHintingMode.None ? SKFontHinting.None : SKFontHinting.Normal
            };
            using var paint = new SKPaint { IsAntialias = true, Color = new SKColor(color.R, color.G, color.B,
                (byte)Math.Clamp(Math.Round(color.A * alpha * opacity), 0, 255)) };
            var indices = new ushort[glyphs.Length]; var positions = new SKPoint[glyphs.Length]; double x = 0;
            for (var index = 0; index < glyphs.Length; index++)
            {
                var glyph = glyphs[index]; indices[index] = glyph.GlyphIndex;
                positions[index] = new((float)((x + glyph.GlyphOffset.X) * density), (float)(glyph.GlyphOffset.Y * density));
                x += glyph.GlyphAdvance;
            }
            var saved = canvas.Save();
            try
            {
                canvas.Scale((float)(1 / density));
                using var builder = new SKTextBlobBuilder();
                var run = builder.AllocatePositionedRun(font, indices.Length);
                run.SetGlyphs(indices); run.SetPositions(positions);
                using var blob = builder.Build();
                canvas.DrawText(blob, (float)(origin.X * density), (float)((origin.Y + baseline) * density), paint);
            }
            finally { canvas.RestoreToCount(saved); }
        }
    }
}
