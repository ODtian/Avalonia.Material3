using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

/// <summary>Template-only indicator: pinned arrow maths, standard busy spinner, or Expressive loading.</summary>
public sealed class MaterialRefreshPresenter : Panel
{
    public static readonly StyledProperty<MaterialPullToRefresh?> RefreshProperty = AvaloniaProperty.Register<MaterialRefreshPresenter, MaterialPullToRefresh?>(nameof(Refresh));
    public MaterialPullToRefresh? Refresh { get => GetValue(RefreshProperty); set => SetValue(RefreshProperty, value); }
    private Border? _standard;
    private MaterialLoadingIndicator? _loading;
    private MaterialLoadingIndicator? _loadingBusy;
    private Border? _loadingContainer;
    private readonly RotateTransform _pullRotation = new();
    private MaterialCircularProgressIndicator? _spinner;
    private RefreshArrow? _arrow;
    private readonly MaterialMotionSettings _motion;
    private readonly MaterialMotionValue _busyMix;
    private readonly MaterialMotionValue _arrowAlpha;
    private bool _initialized;
    public MaterialRefreshPresenter()
    {
        _busyMix = new(this, 0, _ => PaintFeedback());
        _arrowAlpha = new(this, .3, value => { if (_arrow is not null) _arrow.Alpha = value; });
        _motion = new(this, Update);
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Refresh is { } owner) { owner.FeedbackChanged -= Update; owner.FeedbackChanged += Update; }
        Update();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (Refresh is { } owner) owner.FeedbackChanged -= Update;
        _initialized = false;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != RefreshProperty) return;
        if (change.OldValue is MaterialPullToRefresh old) old.FeedbackChanged -= Update;
        if (Refresh is { } refresh) refresh.FeedbackChanged += Update;
        Update();
    }
    private void Update()
    {
        if (Refresh is not { } owner) return;
        if (_standard is null)
        {
            _spinner = new MaterialCircularProgressIndicator { IsIndeterminate = true };
            _spinner.Bind(MaterialCircularProgressIndicator.ForegroundProperty, new DynamicResourceExtension("M3.OnSurfaceVariantBrush"));
            _spinner.TrackBrush = Brushes.Transparent;
            _arrow = new RefreshArrow();
            var content = new Grid();
            content.Children.Add(new LayoutTransformControl { Child = _spinner, LayoutTransform = new ScaleTransform(.4, .4), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
            content.Children.Add(_arrow);
            _standard = new Border { Width = 40, Height = 40, CornerRadius = new CornerRadius(9999), Child = content };
            _standard.Bind(Border.BackgroundProperty, new DynamicResourceExtension("M3.SurfaceContainerHighBrush"));
            _standard.Bind(Border.BoxShadowProperty, new DynamicResourceExtension("M3.Elevation.Shadow2"));
            _loading = new MaterialLoadingIndicator { IsContained = true, IsIndeterminate = false, Width = 48, Height = 48,
                RenderTransform = _pullRotation, RenderTransformOrigin = RelativePoint.Center };
            _loadingBusy = new MaterialLoadingIndicator { IsContained = true, IsIndeterminate = true, Width = 48, Height = 48 };
            _loadingContainer = new Border { Width = 48, Height = 48, CornerRadius = new CornerRadius(24),
                Child = new Grid { Children = { _loading, _loadingBusy } } };
            _loadingContainer.Bind(Border.BackgroundProperty, new DynamicResourceExtension("M3.PrimaryContainerBrush"));
            Children.Add(_standard);
            Children.Add(_loadingContainer);
        }
        var busy = owner.Status is MaterialProgressStatus.Running or MaterialProgressStatus.Paused;
        _standard.IsVisible = !owner.IsExpressive && owner.PresentedDistanceFraction > 0;
        _loadingContainer!.IsVisible = owner.IsExpressive && owner.PresentedDistanceFraction > 0;
        _loadingBusy!.Status = owner.Status == MaterialProgressStatus.Paused ? MaterialProgressStatus.Paused : MaterialProgressStatus.Running;
        _loading!.Value = Math.Min(owner.PresentedDistanceFraction, 1);
        _pullRotation.Angle = -Math.Max(0, owner.PresentedDistanceFraction - 1) * 180;
        _arrow!.Fraction = owner.PresentedDistanceFraction;
        var arrowAlpha = owner.PresentedDistanceFraction >= 1 ? 1 : .3;
        if (!_initialized)
        {
            _busyMix.Snap(busy ? 1 : 0); _arrowAlpha.Snap(arrowAlpha); _initialized = true;
        }
        else
        {
            _busyMix.Spring(busy ? 1 : 0, _motion.DefaultEffects);
            _arrowAlpha.Spring(arrowAlpha, _motion.DefaultEffects);
        }
        PaintFeedback();
        _spinner!.Status = owner.Status == MaterialProgressStatus.Paused ? MaterialProgressStatus.Paused : MaterialProgressStatus.Running;
        InvalidateArrange();
    }
    private void PaintFeedback()
    {
        if (_arrow is null || _spinner is null) return;
        var mix = Math.Clamp(_busyMix.Value, 0, 1);
        _arrow.Opacity = 1 - mix; _arrow.IsVisible = mix < 1;
        _spinner.Opacity = mix; _spinner.IsVisible = mix > 0;
        if (_loading is not null) { _loading.Opacity = 1 - mix; _loading.IsVisible = mix < 1; }
        if (_loadingBusy is not null) { _loadingBusy.Opacity = mix; _loadingBusy.IsVisible = mix > 0; }
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children) child.Measure(new Size(48, 48));
        return new Size(0, 0); // overlay never changes content measurement
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
        {
            var width = child.Width;
            var height = child.Height;
            var offset = Math.Min((Refresh?.PresentedDistanceFraction ?? 0) * (Refresh?.Threshold ?? 80), finalSize.Height);
            child.Arrange(new Rect((finalSize.Width - width) / 2, offset - height, width, height));
        }
        return finalSize;
    }
    private sealed class RefreshArrow : Control
    {
        private double _fraction;
        private double _alpha = .3;
        internal double Alpha { get => _alpha; set { _alpha = value; InvalidateVisual(); } }
        public double Fraction { get => _fraction; set { _fraction = value; InvalidateVisual(); } }
        public override void Render(DrawingContext context)
        {
            var brush = this.TryFindResource("M3.OnSurfaceVariantBrush", ActualThemeVariant, out var resource) && resource is IBrush b ? b : Brushes.Gray;
            var adjusted = Math.Max(0, Math.Min(1, Fraction) - .4) * 5 / 3;
            var linear = Math.Clamp(Fraction - 1, 0, 2);
            var tension = linear - linear * linear / 4;
            var rotation = (-.25 + .4 * adjusted + tension) * .5;
            var sweep = adjusted * .8 * 2 * Math.PI;
            var start = rotation * 2 * Math.PI + rotation * Math.PI / 180;
            var radius = 6.75;
            var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
            var geometry = new StreamGeometry();
            using (var path = geometry.Open())
            {
                for (var i = 0; i <= 48; i++)
                {
                    var angle = start + sweep * i / 48;
                    var point = center + new Vector(Math.Cos(angle), Math.Sin(angle)) * radius;
                    if (i == 0) path.BeginFigure(point, false); else path.LineTo(point);
                }
            }
            using (context.PushOpacity(Math.Clamp(Alpha, 0, 1)))
            {
                context.DrawGeometry(null, new Pen(brush, 2.5, lineCap: PenLineCap.Flat), geometry);
                var arrowAngle = start + sweep - 2.5 * Math.PI / 180;
                Point RotateArrow(double x, double y) => center + new Vector(
                    x * Math.Cos(arrowAngle) - y * Math.Sin(arrowAngle),
                    x * Math.Sin(arrowAngle) + y * Math.Cos(arrowAngle));
                var arrow = new StreamGeometry();
                using (var path = arrow.Open())
                {
                    path.BeginFigure(RotateArrow(radius - 5 * adjusted, -2.5), false);
                    path.LineTo(RotateArrow(radius, 5 * adjusted - 2.5));
                    path.LineTo(RotateArrow(radius + 5 * adjusted, -2.5));
                }
                context.DrawGeometry(null, new Pen(brush, 2.5, lineCap: PenLineCap.Flat), arrow);
            }
        }
    }
}
