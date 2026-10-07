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

internal sealed class MaterialClockLabel(string text) : Control
{
    private MaterialClockNumber? Number => this.GetVisualAncestors().OfType<MaterialClockNumber>().FirstOrDefault();
    private MaterialClockDial? Dial => this.GetVisualAncestors().OfType<MaterialClockDial>().FirstOrDefault();
    private static readonly StyledProperty<IBrush?> SelectedBrushProperty = AvaloniaProperty.Register<MaterialClockLabel, IBrush?>("SelectedBrush");
    private TextLayout? _normal, _selected;
    private MaterialClockNumber? _subscribedNumber;
    private MaterialClockDial? _subscribedDial;
    private (Typeface Typeface, double Size, double Height, double Tracking, IBrush? Normal, IBrush? Selected) _key;

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
    private void ClearLayouts() { _normal?.Dispose(); _selected?.Dispose(); _normal = _selected = null; }
    private void Layouts()
    {
        if (Number is not { } number) return;
        var key = (new Typeface(number.FontFamily, number.FontStyle, number.FontWeight), number.FontSize,
            number.GetValue(TextBlock.LineHeightProperty), number.LetterSpacing, number.Foreground, GetValue(SelectedBrushProperty));
        if (_normal is not null && key == _key) return;
        ClearLayouts(); _key = key;
        _normal = new TextLayout(text, key.Item1, key.Item2, key.Item5, lineHeight: key.Item3, letterSpacing: key.Item4);
        _selected = new TextLayout(text, key.Item1, key.Item2, key.Item6, lineHeight: key.Item3, letterSpacing: key.Item4);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        Layouts(); return _normal is { } layout ? new Size(layout.Width, layout.Height) : default;
    }
    public override void Render(DrawingContext context)
    {
        Layouts();
        if (_normal is not { } normal || _selected is not { } selected) return;
        var origin = new Point((Bounds.Width - normal.Width) / 2, (Bounds.Height - normal.Height) / 2);
        normal.Draw(context, origin);
        if (Dial is not { } dial || dial.TranslatePoint(dial.SelectorCenter, this) is not { } center) return;
        // Pinned drawSelector changes overlapping ink spatially, not the native number's selection.
        using (context.PushGeometryClip(new EllipseGeometry(new Rect(center.X - dial.SelectorRadius, center.Y - dial.SelectorRadius,
                   dial.SelectorRadius * 2, dial.SelectorRadius * 2)))) selected.Draw(context, origin);
    }
}
