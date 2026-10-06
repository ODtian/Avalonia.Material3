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
    private MaterialTextField? _observed;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Observe();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_observed is not null) _observed.PropertyChanged -= FieldChanged;
        _observed = null;
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
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count < 2) return default;
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
        var x = (editor?.Bounds.X ?? 0) + field.Padding.Left;
        var width = Math.Max(0, finalSize.Width - x - 16);
        for (var i = 2; i < Children.Count; i++)
        {
            var child = Children[i];
            var floating = child.Name == "FloatingLabel";
            var y = floating ? (field.Variant == MaterialTextFieldVariant.Outlined ? 0 : field.Padding.Top) :
                top + (containerHeight - child.DesiredSize.Height) / 2;
            child.Arrange(new Rect(x, y, Math.Min(width, child.DesiredSize.Width), child.DesiredSize.Height));
        }
        Children[0].InvalidateVisual();
        return finalSize;
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
        var thickness = field.BorderThickness;
        var stroke = Math.Max(Math.Max(thickness.Left, thickness.Right), Math.Max(thickness.Top, thickness.Bottom));
        if (stroke <= 0 || field.BorderBrush is null) return;
        var pen = new Pen(field.BorderBrush, stroke);
        if (field.Variant == MaterialTextFieldVariant.Filled)
        {
            context.DrawLine(pen, new Point(0, rect.Bottom - stroke / 2), new Point(rect.Right, rect.Bottom - stroke / 2));
            return;
        }
        var outline = rect.Deflate(stroke / 2);
        var label = layout.Children.FirstOrDefault(child => child.Name == "FloatingLabel" && child.IsVisible);
        if (label is null || label.Bounds.Width <= 0)
            context.DrawRectangle(null, pen, new RoundedRect(outline, corner));
        else
        {
            // Actual label rectangle subtracts stroke; no global Surface patch obscures host paint.
            var notch = label.Bounds.Inflate(new Thickness(4, 0));
            using (context.PushClip(new Rect(0, 0, Math.Max(0, notch.Left), Bounds.Height)))
                context.DrawRectangle(null, pen, new RoundedRect(outline, corner));
            using (context.PushClip(new Rect(Math.Min(Bounds.Width, notch.Right), 0, Math.Max(0, Bounds.Width - notch.Right), Bounds.Height)))
                context.DrawRectangle(null, pen, new RoundedRect(outline, corner));
            using (context.PushClip(new Rect(Math.Max(0, notch.Left), notch.Bottom, Math.Max(0, notch.Width), Math.Max(0, Bounds.Height - notch.Bottom))))
                context.DrawRectangle(null, pen, new RoundedRect(outline, corner));
        }
    }
}
