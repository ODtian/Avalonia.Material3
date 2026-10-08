using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Themes;

// Default text-only Adapter. Caller Control/content templates retain the normal presenter path.
internal sealed class ClockNumberTextTemplate : IDataTemplate
{
    public bool Match(object? data) => data is string;
    public Control? Build(object? data) => data is string text ? new MaterialClockLabel(text) : null;
}

internal sealed class MaterialClockLabel : Control
{
    private readonly string _text;
    private MaterialClockNumber? Number => this.GetVisualAncestors().OfType<MaterialClockNumber>().FirstOrDefault();
    private MaterialClockDial? Dial => this.GetVisualAncestors().OfType<MaterialClockDial>().FirstOrDefault();
    private static readonly StyledProperty<IBrush?> SelectedBrushProperty = AvaloniaProperty.Register<MaterialClockLabel, IBrush?>("SelectedBrush");
    private TextLayout? _normal;
    private MaterialNativeText? _nativeText;
    private MaterialSnapshot? _mask;
    private bool _capturingMask;
    private Size _maskSize;
    private double _maskDensity;
    private Point _maskOrigin;
    private TextOptions _maskOptions;
    private TextOptions _measureOptions;
    private AvaloniaObject? _watchedBrush;
    private Transform? _brushTransform;
    private GradientBrush? _gradient;
    private GradientStops? _stopCollection;
    private readonly List<GradientStop> _stops = [];
    private MaterialClockNumber? _subscribedNumber;
    private MaterialClockDial? _subscribedDial;
    private (Typeface Typeface, double Size, double Height, double Tracking, IBrush? Normal, IBrush? Selected) _key;

    internal MaterialClockLabel(string text) { _text = text; UseLayoutRounding = false; }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        MaterialPickerSupport.Resource(this, SelectedBrushProperty, "OnPrimaryBrush");
        _subscribedNumber = Number;
        _subscribedDial = Dial;
        if (_subscribedNumber is { } number) number.PropertyChanged += NumberChanged;
        if (_subscribedDial is { } dial) dial.PropertyChanged += DialChanged;
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // Panel removal has already severed ancestor links when this callback runs.
        if (_subscribedNumber is { } number) number.PropertyChanged -= NumberChanged;
        if (_subscribedDial is { } dial) dial.PropertyChanged -= DialChanged;
        _subscribedNumber = null;
        _subscribedDial = null;
        base.OnDetachedFromVisualTree(e);
        ClearLayouts();
    }
    private void NumberChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBlock.FontFamilyProperty || e.Property == TextBlock.FontSizeProperty ||
            e.Property == TextBlock.FontWeightProperty || e.Property == TextBlock.FontStyleProperty ||
            e.Property == TextBlock.LineHeightProperty || e.Property == TextBlock.LetterSpacingProperty || e.Property == TextBlock.ForegroundProperty)
        { InvalidateMeasure(); InvalidateVisual(); }
    }
    private void DialChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    { InvalidateVisual(); }
    private TextOptions Options()
    {
        var options = new TextOptions();
        foreach (var visual in this.GetVisualAncestors().Reverse().Append(this)) options = TextOptions.GetTextOptions(visual).MergeWith(options);
        return DefaultRendering(options);
    }
    private static TextOptions DefaultRendering(TextOptions options) => options.TextRenderingMode == TextRenderingMode.Unspecified
        ? options with { TextRenderingMode = TextRenderingMode.Antialias } : options;
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedBrushProperty) InvalidateVisual();
    }
    private void ClearLayouts()
    {
        _nativeText?.Dispose(); _nativeText = null;
        _normal?.Dispose(); _normal = null; _mask?.Dispose(); _mask = null;
        if (_watchedBrush is not null) _watchedBrush.PropertyChanged -= BrushInvalidated;
        if (_brushTransform is not null) _brushTransform.Changed -= TransformChanged;
        if (_stopCollection is not null) _stopCollection.CollectionChanged -= StopsChanged;
        foreach (var stop in _stops) stop.PropertyChanged -= BrushInvalidated;
        _stops.Clear(); _gradient = null; _stopCollection = null;
        _watchedBrush = null; _brushTransform = null;
    }
    private void BrushInvalidated(object? sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.Property == GradientBrush.GradientStopsProperty) WatchStops();
        if (args.Property == Brush.TransformProperty) WatchTransform();
        _mask?.Dispose(); _mask = null; InvalidateVisual();
    }
    private void WatchTransform()
    {
        if (_brushTransform is not null) _brushTransform.Changed -= TransformChanged;
        _brushTransform = (_watchedBrush as Brush)?.Transform as Transform;
        if (_brushTransform is not null) _brushTransform.Changed += TransformChanged;
    }
    private void TransformChanged(object? sender, EventArgs args)
    { _mask?.Dispose(); _mask = null; InvalidateVisual(); }
    private void WatchStops()
    {
        if (_stopCollection is not null) _stopCollection.CollectionChanged -= StopsChanged;
        foreach (var stop in _stops) stop.PropertyChanged -= BrushInvalidated;
        _stops.Clear(); _stopCollection = _gradient?.GradientStops;
        if (_stopCollection is null) return;
        _stopCollection.CollectionChanged += StopsChanged;
        foreach (var stop in _stopCollection) { _stops.Add(stop); stop.PropertyChanged += BrushInvalidated; }
    }
    private void StopsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs args)
    {
        WatchStops();
        _mask?.Dispose(); _mask = null; InvalidateVisual();
    }
    private void Layouts()
    {
        if (Number is not { } number) return;
        var key = (new Typeface(number.FontFamily, number.FontStyle, number.FontWeight), number.FontSize,
            number.GetValue(TextBlock.LineHeightProperty), number.LetterSpacing, number.Foreground, GetValue(SelectedBrushProperty));
        if (_normal is not null && key == _key) return;
        ClearLayouts(); _key = key;
        _watchedBrush = key.Item5 as AvaloniaObject;
        if (_watchedBrush is not null) _watchedBrush.PropertyChanged += BrushInvalidated;
        WatchTransform();
        _gradient = key.Item5 as GradientBrush;
        WatchStops();
        _normal = new TextLayout(_text, key.Item1, key.Item2, key.Item5, lineHeight: key.Item3, letterSpacing: key.Item4);
        _nativeText = MaterialNativeText.TryCreate(_text, _normal, key.Item4);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        Layouts();
        var density = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        _measureOptions = Options();
        return _normal is { } layout ? _nativeText?.Measure(density, layout.Height, _key.Normal, _measureOptions)
            ?? new Size(Math.Ceiling(layout.Width * density) / density, Math.Ceiling(layout.Height * density) / density) : default;
    }
    internal void ReconcileTextOptions(TextOptions inherited)
    {
        foreach (var visual in this.GetVisualAncestors().TakeWhile(visual => visual is not MaterialClockDial).Reverse().Append(this))
            inherited = TextOptions.GetTextOptions(visual).MergeWith(inherited);
        inherited = DefaultRendering(inherited);
        if (!_measureOptions.Equals(inherited)) { InvalidateMeasure(); InvalidateVisual(); }
    }
    public override void Render(DrawingContext context)
    {
        Layouts();
        if (_normal is not { } normal) return;
        // Android's offscreen selector mask uses grayscale glyph coverage. Both
        // complementary regions need the same coverage, independent of brush colour.
        var options = Options();
        using var textOptions = context.PushTextOptions(options);
        // Compose places an integer-sized paragraph at an integer centre offset;
        // paragraph ink starts at its measured top-left, including trailing advance.
        var density = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        var point = TopLevel.GetTopLevel(this) is { } root ? this.TranslatePoint(default, root) : null;
        var origin = point is { } position ? new Point(
            Math.Round(position.X * density, MidpointRounding.AwayFromZero) / density - position.X,
            Math.Round(position.Y * density, MidpointRounding.AwayFromZero) / density - position.Y) : default;
        void DrawNormal()
        {
            if (_nativeText?.Draw(context, _key.Normal, origin, density, Bounds.Size, options) != true) normal.Draw(context, origin);
        }
        if (_capturingMask) { DrawNormal(); return; }
        if (Dial is not { } dial || dial.TranslatePoint(dial.SelectorCenter, this) is not { } center)
        { DrawNormal(); return; }
        var radius = dial.SelectorRadius;
        var nearestX = center.X - Math.Clamp(center.X, 0, Bounds.Width);
        var nearestY = center.Y - Math.Clamp(center.Y, 0, Bounds.Height);
        if (nearestX * nearestX + nearestY * nearestY >= radius * radius)
        { DrawNormal(); return; }
        if (_mask is null || _maskSize != Bounds.Size || _maskDensity != density || _maskOrigin != origin || !_maskOptions.Equals(options))
        {
            _mask?.Dispose(); _mask = null; _capturingMask = true;
            try { _mask = MaterialSnapshot.Capture(this, new Rect(Bounds.Size)); }
            finally { _capturingMask = false; }
            _maskSize = Bounds.Size; _maskDensity = density; _maskOrigin = origin; _maskOptions = options;
        }
        if (_mask is null) return;
        // Native draws the normal glyph first, then recolours that alpha through
        // XOR/DstOver. Reuse its cached coverage rather than rasterizing white text.
        var selector = new EllipseGeometry(new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2));
        var outside = new GeometryGroup { FillRule = FillRule.EvenOdd,
            Children = { new RectangleGeometry(new Rect(Bounds.Size)), selector } };
        using (context.PushGeometryClip(outside)) DrawNormal();
        using (context.PushGeometryClip(new EllipseGeometry(selector.Rect)))
        using (_mask.OpacityMask(context, new Rect(Bounds.Size)))
            context.DrawRectangle(_key.Selected, null, new Rect(Bounds.Size));
    }
}
