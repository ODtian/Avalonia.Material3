using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

// Selection remains a synchronous native state. Only capsule/underline paint consumes frame time.
internal sealed class MaterialNavigationSelectionPresenter : Control
{
    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<MaterialNavigationSelectionPresenter, bool>(nameof(IsSelected));
    public static readonly StyledProperty<IBrush?> BrushProperty = AvaloniaProperty.Register<MaterialNavigationSelectionPresenter, IBrush?>(nameof(Brush));
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty = Border.CornerRadiusProperty.AddOwner<MaterialNavigationSelectionPresenter>();
    private static readonly StyledProperty<TimeSpan> DurationProperty = AvaloniaProperty.Register<MaterialNavigationSelectionPresenter, TimeSpan>("Duration");
    private static readonly StyledProperty<IEasing> EasingProperty = AvaloniaProperty.Register<MaterialNavigationSelectionPresenter, IEasing>("Easing", new SplineEasing(.2, 0, 0, 1));
    private readonly MaterialFrameLease _frames;
    private IBrush? _paintBrush;
    private double _value, _from, _target;
    private bool _attached;
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    public IBrush? Brush { get => GetValue(BrushProperty); set => SetValue(BrushProperty, value); }
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    public MaterialNavigationSelectionPresenter()
    {
        IsHitTestVisible = false; UseLayoutRounding = false;
        _frames = MaterialRenderFrames.Bind(this, Advance);
        MaterialPickerSupport.Resource(this, DurationProperty, "StateLayerDuration");
        MaterialPickerSupport.Resource(this, EasingProperty, "Motion.EasingStandard");
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e); _attached = true;
        _paintBrush = Brush; Snap();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attached = false; _frames.SetRunning(false); base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BrushProperty)
        {
            // Deselect styles immediately become Transparent, but exiting ink keeps its last role.
            if (IsSelected || _value == 0 || Brush is not null and not ISolidColorBrush || Brush is ISolidColorBrush { Color.A: > 0 }) _paintBrush = Brush;
            InvalidateVisual();
        }
        else if (change.Property == IsSelectedProperty) Retarget();
        else if (change.Property == DurationProperty && GetValue(DurationProperty) <= TimeSpan.Zero) Snap();
        else if (change.Property == CornerRadiusProperty || change.Property == EasingProperty) InvalidateVisual();
    }
    private void Snap()
    {
        _target = _value = IsSelected ? 1 : 0;
        _frames?.SetRunning(false); InvalidateVisual();
    }
    private void Retarget()
    {
        if (!_attached || GetValue(DurationProperty) <= TimeSpan.Zero) { Snap(); return; }
        _frames.Sample(); _from = _value; _target = IsSelected ? 1 : 0;
        if (_from == _target) return;
        _frames.Restart(); _frames.SetRunning(true); InvalidateVisual();
    }
    private bool Advance(MaterialFrame frame)
    {
        var duration = GetValue(DurationProperty).TotalSeconds;
        var fraction = duration <= 0 ? 1 : Math.Clamp(frame.Elapsed.TotalSeconds / duration, 0, 1);
        _value = fraction == 1 ? _target : _from + (_target - _from) * GetValue(EasingProperty).Ease(fraction);
        InvalidateVisual(); return fraction < 1;
    }
    public override void Render(DrawingContext context)
    {
        if (_value <= 0 || _paintBrush is null) return;
        var width = Bounds.Width * (.6 + .4 * _value);
        var rect = new Rect((Bounds.Width - width) / 2, 0, width, Bounds.Height);
        using (context.PushOpacity(_value)) context.DrawRectangle(_paintBrush, null, new RoundedRect(rect, CornerRadius));
    }
}
