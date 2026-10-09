using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Presenters;
using Avalonia.LogicalTree;
using Avalonia.Material3.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Themes;

internal sealed class MaterialActionContentPresenter : ContentPresenter
{
    private static readonly ExtendedFabTextTemplate DefaultText = new();
    static MaterialActionContentPresenter() => ContentTemplateProperty.OverrideMetadata<MaterialActionContentPresenter>(
        new StyledPropertyMetadata<IDataTemplate?>(coerce: (owner, value) =>
        {
            var presenter = (MaterialActionContentPresenter)owner;
            return value ?? (presenter.Content is string text && !(presenter.RecognizesAccessKey && text.Contains('_'))
                && presenter.FindDataTemplate(presenter.Content, null) is null ? DefaultText : null);
        }));
    protected override Type StyleKeyOverride => typeof(ContentPresenter);
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentProperty || change.Property == TemplatedParentProperty || change.Property == RecognizesAccessKeyProperty) CoerceValue(ContentTemplateProperty);
    }
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        CoerceValue(ContentTemplateProperty);
        base.OnAttachedToLogicalTree(e);
    }
}

internal sealed class ExtendedFabTextTemplate : IDataTemplate
{
    public bool Match(object? data) => data is string;
    public Control? Build(object? data) => data is string text ? new MaterialActionLabel { Text = text, TextWrapping = TextWrapping.Wrap } : null;
}

// The ordinary TextBlock owns the full caller text/style contract. Its protected
// paint seam supplies the locked single-line native recipe without bypassing it.
internal sealed class MaterialActionLabel : TextBlock
{
    private MaterialNativeText? _native;
    private TextOptions _measuredOptions;
    private bool _nativeRoute;
    private MaterialExtendedFab? _owner;
    private MaterialButtonGroup? _groupOwner;
    protected override Type StyleKeyOverride => typeof(TextBlock);
    private TextOptions Options()
    {
        var options = new TextOptions();
        foreach (var visual in this.GetVisualAncestors().Reverse().Append(this))
            options = TextOptions.GetTextOptions(visual).MergeWith(options);
        return options;
    }
    internal void ReconcileTextOptions()
    {
        if (!_measuredOptions.Equals(Options())) { InvalidateMeasure(); InvalidateVisual(); }
    }
    private bool StandardParagraph => FlowDirection == FlowDirection.LeftToRight && TextAlignment is TextAlignment.Start or TextAlignment.Left
        && TextTrimming == TextTrimming.None && MaxLines == 0 && Padding == default && LineSpacing == 0
        && (FontFeatures is null || FontFeatures.Count == 0) && (TextDecorations is null || TextDecorations.Count == 0)
        && (Inlines is null || Inlines.Count == 0) && VerticalAlignment == Avalonia.Layout.VerticalAlignment.Stretch;
    protected override Size MeasureOverride(Size availableSize)
    {
        var ordinary = base.MeasureOverride(availableSize);
        _native?.Dispose(); _native = null;
        if (StandardParagraph) _native = MaterialNativeText.TryCreateAction(Text ?? "", TextLayout, LetterSpacing);
        _measuredOptions = Options();
        var density = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        var measured = _native?.Measure(density, TextLayout.Height, Foreground, _measuredOptions);
        _nativeRoute = measured is { } size && size.Width <= availableSize.Width;
        return _nativeRoute ? measured!.Value : ordinary;
    }
    protected override Size ArrangeOverride(Size finalSize) => _nativeRoute ? finalSize : base.ArrangeOverride(finalSize);
    protected override void RenderTextLayout(DrawingContext context, Point origin)
    {
        var options = Options();
        using var scope = context.PushTextOptions(options);
        var density = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        if (!_nativeRoute || _native?.Draw(context, Foreground, origin, density, Bounds.Size, options) != true)
            base.RenderTextLayout(context, origin);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ForegroundProperty) InvalidateMeasure();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = this.GetVisualAncestors().OfType<MaterialExtendedFab>().FirstOrDefault();
        _owner?.RegisterTextLabel(this);
        if (_owner is null) _groupOwner = this.GetVisualAncestors().OfType<MaterialButtonGroup>().FirstOrDefault();
        _groupOwner?.RegisterTextLabel(this);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _owner?.UnregisterTextLabel(this); _owner = null;
        _groupOwner?.UnregisterTextLabel(this); _groupOwner = null;
        _native?.Dispose(); _native = null;
        base.OnDetachedFromVisualTree(e);
    }
}
