using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

// Private presentation Module. The native editor is a child, never scaled or transformed.
internal sealed class MaterialTextFieldDecoration : Panel
{
    public static readonly StyledProperty<MaterialTextField?> FieldProperty =
        AvaloniaProperty.Register<MaterialTextFieldDecoration, MaterialTextField?>(nameof(Field));
    public static readonly StyledProperty<double> LabelLineHeightProperty =
        AvaloniaProperty.Register<MaterialTextFieldDecoration, double>(nameof(LabelLineHeight), 16);
    public MaterialTextField? Field { get => GetValue(FieldProperty); set => SetValue(FieldProperty, value); }
    public double LabelLineHeight { get => GetValue(LabelLineHeightProperty); set => SetValue(LabelLineHeightProperty, value); }
    internal double OutlineTop => Field is { Variant: MaterialTextFieldVariant.Outlined, Label: { } label } &&
        !string.IsNullOrWhiteSpace(label) ? LabelLineHeight / 2 : 0;
    public static readonly StyledProperty<double> LabelEnvelopeHeightProperty =
        AvaloniaProperty.Register<MaterialTextFieldDecoration, double>(nameof(LabelEnvelopeHeight), 16);
    public double LabelEnvelopeHeight => GetValue(LabelEnvelopeHeightProperty);
    private readonly TextBlock _labelMetrics = new();
    public static readonly StyledProperty<double> LabelProgressProperty =
        AvaloniaProperty.Register<MaterialTextFieldDecoration, double>(nameof(LabelProgress));
    public static readonly StyledProperty<double> StrokeWidthProperty =
        AvaloniaProperty.Register<MaterialTextFieldDecoration, double>(nameof(StrokeWidth), 1);
    public static readonly StyledProperty<IBrush?> StrokeBrushProperty =
        AvaloniaProperty.Register<MaterialTextFieldDecoration, IBrush?>(nameof(StrokeBrush));
    public static readonly StyledProperty<IBrush?> LabelBrushProperty =
        AvaloniaProperty.Register<MaterialTextFieldDecoration, IBrush?>(nameof(LabelBrush));
    public static readonly StyledProperty<IBrush?> AnimatedLabelBrushProperty =
        AvaloniaProperty.Register<MaterialTextFieldDecoration, IBrush?>(nameof(AnimatedLabelBrush));
    public IBrush? LabelBrush { get => GetValue(LabelBrushProperty); set => SetValue(LabelBrushProperty, value); }
    public IBrush? AnimatedLabelBrush => GetValue(AnimatedLabelBrushProperty);
    internal double LabelProgress => GetValue(LabelProgressProperty);
    internal double StrokeWidth => GetValue(StrokeWidthProperty);
    internal IBrush? StrokeBrush => GetValue(StrokeBrushProperty);
    private MaterialTextField? _observed;
    private readonly MaterialMotionSettings _motion;
    private readonly MaterialMotionValue _labelMotion;
    private readonly MaterialMotionValue _strokeMotion;
    private readonly MaterialMotionValue _placeholderMotion;
    private readonly MaterialMotionValue _affixMotion;
    private readonly MaterialMotionBrush _strokeColor;
    private readonly MaterialMotionBrush _labelColor;
    private bool _initialized;
    private int _phase;
    private readonly MatrixTransform _restTransform = new();
    private readonly MatrixTransform _floatTransform = new();

    public MaterialTextFieldDecoration()
    {
        _labelMotion = new(this, 0, value => SetValue(LabelProgressProperty, value));
        _strokeMotion = new(this, 1, value => SetValue(StrokeWidthProperty, value));
        _placeholderMotion = new(this, 0, value => PaintOpacity("PlaceholderHost", value));
        _affixMotion = new(this, 0, value => { PaintOpacity("Prefix", value); PaintOpacity("Suffix", value); });
        _strokeColor = new(this, null, value => SetValue(StrokeBrushProperty, value));
        _labelColor = new(this, null, value => SetValue(AnimatedLabelBrushProperty, value));
        _motion = new(this, Retarget);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Observe();
        Retarget();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_observed is not null) _observed.PropertyChanged -= FieldChanged;
        _observed = null;
        _initialized = false;
        Transitions = null;
        base.OnDetachedFromVisualTree(e);
    }
    private void Observe()
    {
        if (!this.IsAttachedToVisualTree() || _observed == Field) return;
        if (_observed is not null) _observed.PropertyChanged -= FieldChanged;
        _observed = Field;
        if (_observed is not null) _observed.PropertyChanged += FieldChanged;
    }
    private void FieldChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        Retarget();
        if (Children.Count > 0) Children[0].InvalidateVisual();
        if (e.Property == MaterialTextField.VariantProperty || e.Property == MaterialTextField.LabelProperty ||
            e.Property == TextBox.PaddingProperty || e.Property == TextBox.FontSizeProperty || e.Property == TextBox.LineHeightProperty)
            InvalidateMeasure();
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FieldProperty) Observe();
        if (change.Property == LabelBrushProperty) Retarget();
        if (change.Property == FieldProperty || change.Property == LabelLineHeightProperty) InvalidateMeasure();
        if (change.Property == LabelProgressProperty) ProjectLabel();
        if (change.Property == LabelProgressProperty || change.Property == StrokeWidthProperty || change.Property == StrokeBrushProperty)
            if (Children.Count > 0) Children[0].InvalidateVisual();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count < 2) return default;
        if (Field is { Variant: MaterialTextFieldVariant.Filled } field && !string.IsNullOrWhiteSpace(field.Label) &&
            Children.OfType<TextBlock>().FirstOrDefault(child => child.Name == "FloatingLabel") is { } floating)
        {
            var lanes = Children[1] as Panel;
            var reservedWidth = 0d;
            if (lanes is not null)
                foreach (var lane in lanes.Children.Where(child => Grid.GetColumn(child) != 1 && child.IsVisible))
                {
                    lane.Measure(availableSize);
                    reservedWidth += lane.DesiredSize.Width;
                }
            _labelMetrics.Text = field.Label;
            _labelMetrics.FontFamily = floating.FontFamily;
            _labelMetrics.FontSize = floating.FontSize;
            _labelMetrics.FontWeight = floating.FontWeight;
            _labelMetrics.FontStyle = floating.FontStyle;
            _labelMetrics.FontStretch = floating.FontStretch;
            _labelMetrics.LineHeight = floating.LineHeight;
            _labelMetrics.LetterSpacing = floating.LetterSpacing;
            _labelMetrics.TextWrapping = TextWrapping.Wrap;
            _labelMetrics.Measure(new Size(Math.Max(0, availableSize.Width - reservedWidth - field.Padding.Left - field.Padding.Right), double.PositiveInfinity));
            SetValue(LabelEnvelopeHeightProperty, Math.Max(LabelLineHeight, _labelMetrics.DesiredSize.Height));
        }
        else SetValue(LabelEnvelopeHeightProperty, LabelLineHeight);
        Children[1].Measure(new Size(availableSize.Width, Math.Max(0, availableSize.Height - OutlineTop)));
        for (var i = 2; i < Children.Count; i++)
            Children[i].Measure(new Size(Math.Max(0, availableSize.Width - 32), double.PositiveInfinity));
        return new Size(Children[1].DesiredSize.Width, OutlineTop + Math.Max(56, Children[1].DesiredSize.Height));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count < 2 || Field is not { } field) return finalSize;
        var top = OutlineTop;
        var containerHeight = finalSize.Height - top;
        Children[0].Arrange(new Rect(finalSize));
        Children[1].Arrange(new Rect(0, top, finalSize.Width, containerHeight));
        var content = Children[1] as Panel;
        var editor = content?.Children.FirstOrDefault(child => child.Name == "EditorDock");
        var x = editor?.Bounds.X ?? field.Padding.Left;
        var width = Math.Max(0, editor?.Bounds.Width ?? finalSize.Width - x - 16);
        for (var i = 2; i < Children.Count; i++)
        {
            var child = Children[i];
            var floating = child.Name == "FloatingLabel";
            var y = floating ? (field.Variant == MaterialTextFieldVariant.Outlined ? 0 : field.Padding.Top) :
                top + (containerHeight - child.DesiredSize.Height) / 2;
            child.Measure(new Size(width, double.PositiveInfinity));
            child.Arrange(new Rect(x, y, Math.Min(width, child.DesiredSize.Width), child.DesiredSize.Height));
        }
        ProjectLabel(finalSize.Height);
        ConfigureDecorationTransitions();
        Children[0].InvalidateVisual();
        return finalSize;
    }
    private void Retarget()
    {
        if (Field is not { } field || _motion is null) return;
        var phase = field.IsFocused ? 1 : string.IsNullOrEmpty(field.Text) ? 0 : 2;
        var label = phase == 0 ? 0 : 1;
        var thickness = field.BorderThickness;
        var stroke = Math.Max(Math.Max(thickness.Left, thickness.Right), Math.Max(thickness.Top, thickness.Bottom));
        var placeholder = phase == 1 || string.IsNullOrWhiteSpace(field.Label) ? 1 : 0;
        var affix = phase != 0 || string.IsNullOrWhiteSpace(field.Label) ? 1 : 0;
        if (!_initialized || !field.IsEffectivelyEnabled)
        {
            _labelMotion.Snap(label); _strokeMotion.Snap(stroke); _strokeColor.Snap(field.BorderBrush);
            _labelColor.Snap(LabelBrush);
            _placeholderMotion.Snap(placeholder); _affixMotion.Snap(affix);
            _initialized = true;
        }
        else
        {
            _labelMotion.Spring(label, _motion.FastSpatial);
            _strokeMotion.Spring(stroke, _motion.FastSpatial);
            _strokeColor.Set(field.BorderBrush, _motion.FastEffects);
            _labelColor.Set(LabelBrush, _motion.FastEffects);
            var opacitySpec = (_phase == 0 && phase == 1 || _phase == 2 && phase == 0)
                ? _motion.SlowEffects : _motion.FastEffects;
            _placeholderMotion.Spring(placeholder, opacitySpec);
            _affixMotion.Spring(affix, _motion.FastEffects);
        }
        _phase = phase;
    }
    private void PaintOpacity(string name, double value)
    {
        if (Children.Count < 2) return;
        var control = Children[1].GetVisualDescendants().OfType<Control>().FirstOrDefault(child => child.Name == name);
        if (control is not null) control.Opacity = Math.Clamp(value, 0, 1);
    }
    private void ConfigureDecorationTransitions()
    {
        if (Children.Count < 2) return;
        foreach (var control in Children[1].GetVisualDescendants().OfType<Control>().Where(control => control.Name is "Prefix" or "Suffix" or "PlaceholderHost"))
            control.Transitions = null;
        PaintOpacity("PlaceholderHost", _placeholderMotion.Value);
        PaintOpacity("Prefix", _affixMotion.Value); PaintOpacity("Suffix", _affixMotion.Value);
    }
    private void ProjectLabel(double? arrangedHeight = null)
    {
        if (Field is not { } field) return;
        var resting = Children.OfType<TextBlock>().FirstOrDefault(child => child.Name == "RestingLabel");
        var floating = Children.OfType<TextBlock>().FirstOrDefault(child => child.Name == "FloatingLabel");
        if (resting is null || floating is null || floating.FontSize <= 0 || resting.FontSize <= 0) return;
        var p = Math.Clamp(LabelProgress, 0, 1);
        var lineHeight = double.IsFinite(field.LineHeight) && field.LineHeight > 0 ? field.LineHeight : field.FontSize * 1.5;
        // The framework commits this panel's Bounds after ArrangeOverride returns.
        var restY = OutlineTop + ((arrangedHeight ?? Bounds.Height) - OutlineTop - lineHeight) / 2;
        var floatY = field.Variant == MaterialTextFieldVariant.Outlined ? 0 : field.Padding.Top;
        var y = restY + (floatY - restY) * p;
        var size = resting.FontSize + (floating.FontSize - resting.FontSize) * p;
        // Only animated decorative glyphs bypass baseline snapping; native editor text keeps host policy.
        TextOptions.SetBaselinePixelAlignment(resting, BaselinePixelAlignment.Unaligned);
        TextOptions.SetBaselinePixelAlignment(floating, BaselinePixelAlignment.Unaligned);
        resting.RenderTransformOrigin = RelativePoint.TopLeft;
        floating.RenderTransformOrigin = RelativePoint.TopLeft;
        resting.RenderTransform = _restTransform;
        floating.RenderTransform = _floatTransform;
        _restTransform.Matrix = Matrix.CreateScale(size / resting.FontSize, size / resting.FontSize) * Matrix.CreateTranslation(0, y - resting.Bounds.Y);
        _floatTransform.Matrix = Matrix.CreateScale(size / floating.FontSize, size / floating.FontSize) * Matrix.CreateTranslation(0, y - floating.Bounds.Y);
    }
}

// Separate Control because Avalonia Panel seals Render; painter is noninteractive decoration.
internal sealed class MaterialTextFieldPainter : Control
{
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (this.GetVisualParent() is not MaterialTextFieldDecoration layout || layout.Field is not { } field ||
            Bounds.Width <= 0 || Bounds.Height <= layout.OutlineTop) return;
        var rect = new Rect(0, layout.OutlineTop, Bounds.Width, Bounds.Height - layout.OutlineTop);
        var corner = field.CornerRadius;
        context.DrawRectangle(field.Background, null, new RoundedRect(rect, corner));
        var stroke = layout.StrokeWidth;
        if (stroke <= 0 || layout.StrokeBrush is null) return;
        var pen = new Pen(layout.StrokeBrush, stroke);
        if (field.Variant == MaterialTextFieldVariant.Filled)
        {
            context.DrawLine(pen, new Point(0, rect.Bottom - stroke / 2), new Point(rect.Right, rect.Bottom - stroke / 2));
            return;
        }
        var outline = rect.Deflate(stroke / 2);
        var label = layout.Children.OfType<TextBlock>().FirstOrDefault(child => child.IsVisible);
        var labelRect = label is not null && label.TransformToVisual(layout) is { } transform
            ? new Rect(label.Bounds.Size).TransformToAABB(transform) : default;
        if (labelRect.Width <= 0 || labelRect.Top > rect.Top + stroke || labelRect.Bottom < rect.Top)
            context.DrawRectangle(null, pen, new RoundedRect(outline, corner));
        else
        {
            // The currently painted label rectangle subtracts stroke, including intermediate frames.
            var notch = labelRect.Inflate(new Thickness(4, 0));
            using (context.PushClip(new Rect(0, 0, Math.Max(0, notch.Left), Bounds.Height)))
                context.DrawRectangle(null, pen, new RoundedRect(outline, corner));
            using (context.PushClip(new Rect(Math.Min(Bounds.Width, notch.Right), 0, Math.Max(0, Bounds.Width - notch.Right), Bounds.Height)))
                context.DrawRectangle(null, pen, new RoundedRect(outline, corner));
            using (context.PushClip(new Rect(Math.Max(0, notch.Left), notch.Bottom, Math.Max(0, notch.Width), Math.Max(0, Bounds.Height - notch.Bottom))))
                context.DrawRectangle(null, pen, new RoundedRect(outline, corner));
        }
    }
}
