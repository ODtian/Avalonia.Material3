using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

/// <summary>A genuine, decorative Google Material Symbols Rounded icon in a fixed DIP square.
/// Symbol names come from the pinned official codepoints; document fonts never affect this visual.
/// Filled selects a real static FILL=1 font instance, not a weight or Unicode substitute.</summary>
public sealed class MaterialSymbol : Control
{
    public static readonly StyledProperty<string> SymbolProperty = AvaloniaProperty.Register<MaterialSymbol, string>(
        nameof(Symbol), "add", validate: value => value is not null && MaterialSymbolCodepoints.TryGet(value, out _));
    public static readonly StyledProperty<double> SizeProperty = AvaloniaProperty.Register<MaterialSymbol, double>(
        nameof(Size), 24, validate: value => double.IsFinite(value) && value > 0);
    public static readonly StyledProperty<bool> FilledProperty = AvaloniaProperty.Register<MaterialSymbol, bool>(nameof(Filled));
    public static readonly StyledProperty<IBrush?> ForegroundProperty = TextElement.ForegroundProperty.AddOwner<MaterialSymbol>();

    public string Symbol { get => GetValue(SymbolProperty); set => SetValue(SymbolProperty, value); }
    public double Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public bool Filled { get => GetValue(FilledProperty); set => SetValue(FilledProperty, value); }
    public IBrush? Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    // Art itself does not inherit a blanket RTL mirror. Only directional assets mirror once below.
    protected override bool BypassFlowDirectionPolicies => true;
    private static readonly Dictionary<(string, bool), Artwork> Cache = [];
    private sealed record Artwork(Geometry Geometry, Vector Centering);
    private static readonly FontFamily Unfilled = new("avares://Avalonia.Material3/Assets/Icons/MaterialSymbolsRounded-Unfilled.ttf#Material Symbols Rounded Unfilled");
    private static readonly FontFamily FilledFace = new("avares://Avalonia.Material3/Assets/Icons/MaterialSymbolsRounded-Filled.ttf#Material Symbols Rounded Filled");

    static MaterialSymbol()
    {
        AffectsMeasure<MaterialSymbol>(SizeProperty);
        AffectsRender<MaterialSymbol>(SymbolProperty, SizeProperty, FilledProperty, ForegroundProperty, FlowDirectionProperty);
        IsHitTestVisibleProperty.OverrideDefaultValue<MaterialSymbol>(false);
        UseLayoutRoundingProperty.OverrideDefaultValue<MaterialSymbol>(false);
        HorizontalAlignmentProperty.OverrideDefaultValue<MaterialSymbol>(Layout.HorizontalAlignment.Center);
        VerticalAlignmentProperty.OverrideDefaultValue<MaterialSymbol>(Layout.VerticalAlignment.Center);
        AutomationProperties.AccessibilityViewProperty.OverrideDefaultValue<MaterialSymbol>(AccessibilityView.Raw);
    }

    protected override Size MeasureOverride(Size availableSize) => new(Size, Size);

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Foreground is null) return;
        var artwork = GetArtwork(Symbol, Filled);
        var scale = Size / 24;
        var x = (Bounds.Width - Size) / 2;
        var y = (Bounds.Height - Size) / 2;
        var directional = Symbol is "arrow_back" or "arrow_forward" or "chevron_left" or "chevron_right" or "undo" or "redo";
        using var frame = context.PushTransform(directional && FlowDirection == FlowDirection.RightToLeft
            ? new Matrix(-scale, 0, 0, scale, x + Size, y) : new Matrix(scale, 0, 0, scale, x, y));
        using var ink = context.PushTransform(Matrix.CreateTranslation(artwork.Centering));
        context.DrawGeometry(Foreground, null, artwork.Geometry);
    }

    private static Artwork GetArtwork(string symbol, bool filled)
    {
        if (Cache.TryGetValue((symbol, filled), out var cached)) return cached;
        MaterialSymbolCodepoints.TryGet(symbol, out var codepoint);
        if (!FontManager.Current.TryGetGlyphTypeface(new Typeface(filled ? FilledFace : Unfilled), out var face)
            || !face.CharacterToGlyphMap.TryGetGlyph(codepoint, out var glyph) || glyph == 0)
            throw new InvalidOperationException($"The embedded Material Symbols asset does not contain '{symbol}'. No document-font fallback is allowed.");
        using var run = new GlyphRun(face, 24, char.ConvertFromUtf32(codepoint).AsMemory(), new ushort[] { glyph }, new Point(0, 24));
        // Outline geometry bypasses hinted text baseline snapping. Preserve nominal em scale;
        // translate ink to frame center, NEVER stretch the tight ink bbox to fill the square.
        var geometry = run.BuildGeometry();
        var bounds = geometry.Bounds;
        var result = new Artwork(geometry, new Vector(12 - bounds.Center.X, 12 - bounds.Center.Y));
        Cache.Add((symbol, filled), result);
        return result;
    }
}
