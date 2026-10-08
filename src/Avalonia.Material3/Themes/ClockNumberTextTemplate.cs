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
    private TextLayout? _normal, _selected, _masked;
    private readonly ClockPalette _palette = new();
    private readonly VisualBrush _ink;
    private MaterialClockNumber? _subscribedNumber;
    private MaterialClockDial? _subscribedDial;
    private (Typeface Typeface, double Size, double Height, double Tracking, IBrush? Normal, IBrush? Selected) _key;

    internal MaterialClockLabel(string text) { _text = text; _ink = new(_palette) { Stretch = Stretch.Fill }; }

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
    private void DialChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => InvalidateVisual();
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedBrushProperty) InvalidateVisual();
    }
    private void ClearLayouts() { _normal?.Dispose(); _selected?.Dispose(); _masked?.Dispose(); _normal = _selected = _masked = null; }
    private void Layouts()
    {
        if (Number is not { } number) return;
        var key = (new Typeface(number.FontFamily, number.FontStyle, number.FontWeight), number.FontSize,
            number.GetValue(TextBlock.LineHeightProperty), number.LetterSpacing, number.Foreground, GetValue(SelectedBrushProperty));
        if (_normal is not null && key == _key) return;
        ClearLayouts(); _key = key;
        _normal = new TextLayout(_text, key.Item1, key.Item2, key.Item5, lineHeight: key.Item3, letterSpacing: key.Item4);
        _selected = new TextLayout(_text, key.Item1, key.Item2, key.Item6, lineHeight: key.Item3, letterSpacing: key.Item4);
        _masked = new TextLayout(_text, key.Item1, key.Item2, _ink, lineHeight: key.Item3, letterSpacing: key.Item4);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        Layouts(); return _normal is { } layout ? new Size(layout.Width, layout.Height) : default;
    }
    public override void Render(DrawingContext context)
    {
        Layouts();
        if (_normal is not { } normal || _selected is not { } selected) return;
        // Android's offscreen selector mask uses grayscale glyph coverage. Both
        // complementary regions need the same coverage, independent of brush colour.
        using var textOptions = context.PushTextOptions(new TextOptions { TextRenderingMode = TextRenderingMode.Antialias });
        var origin = new Point((Bounds.Width - normal.Width) / 2, (Bounds.Height - normal.Height) / 2);
        if (Dial is not { } dial || dial.TranslatePoint(dial.SelectorCenter, this) is not { } center)
        { normal.Draw(context, origin); return; }
        var radius = dial.SelectorRadius;
        var furthestX = Math.Max(Math.Abs(center.X), Math.Abs(Bounds.Width - center.X));
        var furthestY = Math.Max(Math.Abs(center.Y), Math.Abs(Bounds.Height - center.Y));
        if (furthestX * furthestX + furthestY * furthestY <= radius * radius)
        { selected.Draw(context, origin); return; }
        var nearestX = center.X - Math.Clamp(center.X, 0, Bounds.Width);
        var nearestY = center.Y - Math.Clamp(center.Y, 0, Bounds.Height);
        if (nearestX * nearestX + nearestY * nearestY >= radius * radius)
        { normal.Draw(context, origin); return; }
        // Pinned drawSelector changes overlapping ink spatially, not the native number's selection.
        // A single glyph pass applies the spatial palette, so antialiased edges composite once.
        _palette.Normal = _key.Normal; _palette.Selected = _key.Selected;
        _palette.Center = center - origin; _palette.Radius = radius;
        var size = new Size(normal.Width, normal.Height);
        _palette.Measure(size); _palette.Arrange(new Rect(size));
        _ink.SourceRect = new RelativeRect(new Rect(size), RelativeUnit.Absolute);
        _ink.DestinationRect = new RelativeRect(new Rect(size), RelativeUnit.Absolute);
        _palette.InvalidateVisual(); _masked!.Draw(context, origin);
    }

    private sealed class ClockPalette : Control
    {
        internal IBrush? Normal, Selected;
        internal Point Center;
        internal double Radius;
        public override void Render(DrawingContext context)
        {
            context.DrawRectangle(Normal, null, new Rect(Bounds.Size));
            context.DrawEllipse(Selected, null, Center, Radius, Radius);
        }
    }
}
