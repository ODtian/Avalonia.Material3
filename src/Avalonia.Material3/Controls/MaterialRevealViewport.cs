using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

// The foreground obeys the reveal viewport; decorative elevation paints outside it.
internal abstract class MaterialRevealViewport : Decorator, IMaterialPaintOverflow
{
    public static readonly StyledProperty<BoxShadows> ElevationShadowProperty = AvaloniaProperty.Register<MaterialRevealViewport, BoxShadows>(nameof(ElevationShadow));
    public static readonly StyledProperty<CornerRadius> ShadowCornerRadiusProperty = AvaloniaProperty.Register<MaterialRevealViewport, CornerRadius>(nameof(ShadowCornerRadius));
    public BoxShadows ElevationShadow { get => GetValue(ElevationShadowProperty); set => SetValue(ElevationShadowProperty, value); }
    public CornerRadius ShadowCornerRadius { get => GetValue(ShadowCornerRadiusProperty); set => SetValue(ShadowCornerRadiusProperty, value); }
    private MaterialClipLease? _foregroundClip;
    static MaterialRevealViewport() => AffectsRender<MaterialRevealViewport>(ElevationShadowProperty, ShadowCornerRadiusProperty);
    protected MaterialRevealViewport() { ClipToBounds = true; UseLayoutRounding = false; }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ChildProperty) ReleaseClip();
        if (change.Property != ElevationShadowProperty) return;
        SetCurrentValue(ClipToBoundsProperty, ElevationShadow == default);
        if (ElevationShadow == default) ReleaseClip();
        InvalidateArrange();
    }
    protected void UpdateForegroundClip(Size size)
    {
        if (ElevationShadow == default || Child is not { } child) return;
        var mask = new RectangleGeometry(new Rect(size).TransformToAABB(this.TransformToVisual(child)!.Value));
        if (_foregroundClip?.Child != child) { ReleaseClip(); _foregroundClip = new(child, mask); }
        else _foregroundClip.Update(mask);
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    { base.OnAttachedToVisualTree(e); InvalidateArrange(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    { ReleaseClip(); base.OnDetachedFromVisualTree(e); }
    private void ReleaseClip() { _foregroundClip?.Dispose(); _foregroundClip = null; }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (ElevationShadow == default) return;
        var maximum = Math.Min(Bounds.Width, Bounds.Height) / 2;
        double Radius(double value) => Math.Min(value, maximum);
        var rounded = new RoundedRect(new Rect(Bounds.Size), Radius(ShadowCornerRadius.TopLeft), Radius(ShadowCornerRadius.TopRight),
            Radius(ShadowCornerRadius.BottomRight), Radius(ShadowCornerRadius.BottomLeft));
        context.DrawRectangle(null, null, rounded, ElevationShadow);
    }
    Rect IMaterialPaintOverflow.GetPaintBounds(Rect bounds) => ElevationShadow.TransformBounds(bounds);
}
