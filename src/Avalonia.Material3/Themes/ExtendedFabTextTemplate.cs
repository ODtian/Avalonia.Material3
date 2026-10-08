using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Themes;

internal sealed class ExtendedFabTextTemplate : IDataTemplate
{
    public bool Match(object? data) => data is string;
    public Control? Build(object? data) => data is string text ? new MaterialActionLabel(text) : null;
}

// Only the locked Latin face uses native integer advances; other caller fonts,
// wrapping and custom brushes retain the public TextLayout shaping path.
internal sealed class MaterialActionLabel(string text) : Control
{
    private TextLayout? _layout;
    private MaterialNativeText? _native;
    private TextOptions _measuredOptions;
    private Size _constraint;
    private bool _nativeRoute;
    private MaterialExtendedFab? _owner;
    private (Typeface Typeface, double Size, double Height, double Tracking, IBrush? Brush, Size Constraint) _key;
    static MaterialActionLabel()
    {
        AffectsMeasure<MaterialActionLabel>(TextBlock.FontFamilyProperty, TextBlock.FontSizeProperty, TextBlock.FontWeightProperty,
            TextBlock.FontStyleProperty, TextBlock.LineHeightProperty, TextBlock.LetterSpacingProperty, TextBlock.ForegroundProperty);
        AffectsRender<MaterialActionLabel>(TextBlock.ForegroundProperty);
    }
    private TextOptions Options()
    {
        var options = new TextOptions();
        foreach (var visual in this.GetVisualAncestors().Reverse().Append(this))
            options = TextOptions.GetTextOptions(visual).MergeWith(options);
        return options with { TextRenderingMode = TextRenderingMode.Antialias };
    }
    internal void ReconcileTextOptions()
    {
        if (!_measuredOptions.Equals(Options())) { InvalidateMeasure(); InvalidateVisual(); }
    }
    private void Layouts(Size constraint)
    {
        var key = (new Typeface(GetValue(TextBlock.FontFamilyProperty), GetValue(TextBlock.FontStyleProperty), GetValue(TextBlock.FontWeightProperty)),
            GetValue(TextBlock.FontSizeProperty), GetValue(TextBlock.LineHeightProperty), GetValue(TextBlock.LetterSpacingProperty), GetValue(TextBlock.ForegroundProperty), constraint);
        if (_layout is not null && key == _key) return;
        _native?.Dispose(); _layout?.Dispose(); _key = key;
        _layout = new TextLayout(text, key.Item1, key.Item2, key.Item5, textWrapping: TextWrapping.Wrap,
            flowDirection: FlowDirection, maxWidth: constraint.Width, maxHeight: constraint.Height, lineHeight: key.Item3, letterSpacing: key.Item4);
        _native = MaterialNativeText.TryCreateAction(text, _layout, key.Item4);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        _constraint = availableSize; Layouts(availableSize);
        _measuredOptions = Options();
        var density = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        var measured = _native?.Measure(density, _layout!.Height, _key.Brush, _measuredOptions);
        _nativeRoute = measured is { } size && size.Width <= availableSize.Width;
        return _nativeRoute ? measured!.Value : new Size(Math.Ceiling(_layout!.Width * density) / density, Math.Ceiling(_layout.Height * density) / density);
    }
    public override void Render(DrawingContext context)
    {
        Layouts(_constraint);
        var options = Options();
        using var scope = context.PushTextOptions(options);
        var density = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        if (!_nativeRoute || _native?.Draw(context, _key.Brush, default, density, Bounds.Size, options) != true)
            _layout!.Draw(context, default);
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = this.GetVisualAncestors().OfType<MaterialExtendedFab>().FirstOrDefault();
        _owner?.RegisterTextLabel(this);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _owner?.UnregisterTextLabel(this); _owner = null;
        _native?.Dispose(); _native = null; _layout?.Dispose(); _layout = null;
        base.OnDetachedFromVisualTree(e);
    }
}
