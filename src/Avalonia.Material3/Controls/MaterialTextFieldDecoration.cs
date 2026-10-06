using Avalonia.Animation;
using Avalonia.Animation.Easings;
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
    internal double LabelProgress => GetValue(LabelProgressProperty);
    internal double StrokeWidth => GetValue(StrokeWidthProperty);
    internal IBrush? StrokeBrush => GetValue(StrokeBrushProperty);
    private MaterialTextField? _observed;
    private IDisposable? _durationSubscription;
    private IDisposable? _easingSubscription;
    private TimeSpan _duration;
    private Easing _easing = new LinearEasing();
    private readonly MatrixTransform _restTransform = new();
    private readonly MatrixTransform _floatTransform = new();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Observe();
        Retarget();
        _durationSubscription = this.GetResourceObservable("M3.Motion.DurationShort4").Subscribe(new MotionObserver(value =>
        {
            _duration = value is TimeSpan duration ? duration : TimeSpan.Zero;
            UpdateMotion();
        }));
        _easingSubscription = this.GetResourceObservable("M3.Motion.EasingStandard").Subscribe(new MotionObserver(value =>
        {
            _easing = value is Easing easing ? easing : new LinearEasing();
            UpdateMotion();
        }));
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_observed is not null) _observed.PropertyChanged -= FieldChanged;
        _observed = null;
        _durationSubscription?.Dispose(); _durationSubscription = null;
        _easingSubscription?.Dispose(); _easingSubscription = null;
        Transitions = null;
        base.OnDetachedFromVisualTree(e);
    }
    private void Observe()
    {
        if (_observed == Field) return;
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
        ProjectLabel();
        ConfigureDecorationTransitions();
        Children[0].InvalidateVisual();
        return finalSize;
    }
    private void Retarget()
    {
        if (Field is not { } field) return;
        SetValue(LabelProgressProperty, field.IsFocused || !string.IsNullOrEmpty(field.Text) ? 1d : 0d);
        var thickness = field.BorderThickness;
        SetValue(StrokeWidthProperty, Math.Max(Math.Max(thickness.Left, thickness.Right), Math.Max(thickness.Top, thickness.Bottom)));
        SetValue(StrokeBrushProperty, field.BorderBrush);
    }
    private bool _decorationTransitionsDirty = true;
    private void UpdateMotion()
    {
        _decorationTransitionsDirty = true;
        ConfigureDecorationTransitions();
        Transitions = null; // Disposing an active transition reveals its target: live reduced motion snaps.
        Retarget();
        if (_duration > TimeSpan.Zero)
            Transitions = new Transitions
            {
                new DoubleTransition { Property = LabelProgressProperty, Duration = _duration, Easing = _easing },
                new DoubleTransition { Property = StrokeWidthProperty, Duration = _duration, Easing = _easing },
                new BrushTransition { Property = StrokeBrushProperty, Duration = _duration, Easing = _easing }
            };
    }
    private void ConfigureDecorationTransitions()
    {
        if (!_decorationTransitionsDirty || Children.Count < 2) return;
        _decorationTransitionsDirty = false;
        foreach (var control in Children[1].GetVisualDescendants().OfType<Control>().Where(control => control.Name is "Prefix" or "Suffix" or "PlaceholderHost"))
        {
            control.Transitions = null;
            if (_duration > TimeSpan.Zero)
                control.Transitions = new Transitions { new DoubleTransition { Property = OpacityProperty, Duration = _duration, Easing = _easing } };
        }
    }
    private void ProjectLabel()
    {
        if (Field is not { } field) return;
        var resting = Children.OfType<TextBlock>().FirstOrDefault(child => child.Name == "RestingLabel");
        var floating = Children.OfType<TextBlock>().FirstOrDefault(child => child.Name == "FloatingLabel");
        if (resting is null || floating is null || floating.FontSize <= 0 || resting.FontSize <= 0) return;
        var p = Math.Clamp(LabelProgress, 0, 1);
        var lineHeight = double.IsFinite(field.LineHeight) && field.LineHeight > 0 ? field.LineHeight : field.FontSize * 1.5;
        var restY = OutlineTop + (Bounds.Height - OutlineTop - lineHeight) / 2;
        var floatY = field.Variant == MaterialTextFieldVariant.Outlined ? 0 : field.Padding.Top;
        var y = restY + (floatY - restY) * p;
        var size = resting.FontSize + (floating.FontSize - resting.FontSize) * p;
        resting.RenderTransformOrigin = RelativePoint.TopLeft;
        floating.RenderTransformOrigin = RelativePoint.TopLeft;
        resting.RenderTransform = _restTransform;
        floating.RenderTransform = _floatTransform;
        _restTransform.Matrix = Matrix.CreateScale(size / resting.FontSize, size / resting.FontSize) * Matrix.CreateTranslation(0, y - resting.Bounds.Y);
        _floatTransform.Matrix = Matrix.CreateScale(size / floating.FontSize, size / floating.FontSize) * Matrix.CreateTranslation(0, y - floating.Bounds.Y);
    }
    private sealed class MotionObserver(Action<object?> update) : IObserver<object?>
    {
        public void OnNext(object? value) => update(value);
        public void OnCompleted() { }
        public void OnError(Exception error) { }
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
