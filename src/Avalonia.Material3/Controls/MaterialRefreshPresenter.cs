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
    private MaterialCircularProgressIndicator? _spinner;
    private RefreshArrow? _arrow;
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Refresh is { } owner) { owner.FeedbackChanged -= Update; owner.FeedbackChanged += Update; }
        Update();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (Refresh is { } owner) owner.FeedbackChanged -= Update;
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
            _loading = new MaterialLoadingIndicator { IsContained = true, Width = 48, Height = 48 };
            Children.Add(_standard);
            Children.Add(_loading);
        }
        var busy = owner.Status is MaterialProgressStatus.Running or MaterialProgressStatus.Paused;
        _standard.IsVisible = !owner.IsExpressive && owner.DistanceFraction > 0;
        _loading!.IsVisible = owner.IsExpressive && owner.DistanceFraction > 0;
        _loading.Status = busy ? owner.Status : MaterialProgressStatus.Running;
        _loading.IsIndeterminate = busy;
        _loading.Value = Math.Min(owner.DistanceFraction, 1);
        _arrow!.IsVisible = !busy;
        _arrow.Fraction = owner.DistanceFraction;
        _spinner!.IsVisible = busy;
        _spinner.Status = owner.Status == MaterialProgressStatus.Paused ? MaterialProgressStatus.Paused : MaterialProgressStatus.Running;
        InvalidateArrange();
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
            var offset = Math.Min((Refresh?.DistanceFraction ?? 0) * (Refresh?.Threshold ?? 80), finalSize.Height);
            child.Arrange(new Rect((finalSize.Width - width) / 2, offset - height, width, height));
        }
        return finalSize;
    }
    private sealed class RefreshArrow : Control
    {
        private double _fraction;
        public double Fraction { get => _fraction; set { _fraction = value; InvalidateVisual(); } }
        public override void Render(DrawingContext context)
        {
            var brush = this.TryFindResource("M3.OnSurfaceVariantBrush", ActualThemeVariant, out var resource) && resource is IBrush b ? b : Brushes.Gray;
            var adjusted = Math.Max(0, Math.Min(1, Fraction) - .4) * 5 / 3;
            var linear = Math.Clamp(Fraction - 1, 0, 2);
            var tension = linear - linear * linear / 4;
            var rotation = (-.25 + .4 * adjusted + tension) * .5;
            var sweep = adjusted * .8 * 2 * Math.PI;
            var start = rotation * 2 * Math.PI;
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
            using (context.PushOpacity(Fraction >= 1 ? 1 : .3))
            {
                context.DrawGeometry(null, new Pen(brush, 2.5, lineCap: PenLineCap.Round), geometry);
                var end = start + sweep;
                var tip = center + new Vector(Math.Cos(end), Math.Sin(end)) * radius;
                var tangent = new Vector(-Math.Sin(end), Math.Cos(end));
                var radial = new Vector(Math.Cos(end), Math.Sin(end));
                var arrow = new StreamGeometry();
                using (var path = arrow.Open())
                {
                    path.BeginFigure(tip + tangent * (5 * adjusted), true);
                    path.LineTo(tip - tangent * (5 * adjusted) + radial * (5 * adjusted));
                    path.LineTo(tip - tangent * (5 * adjusted) - radial * (5 * adjusted));
                    path.EndFigure(true);
                }
                context.DrawGeometry(brush, null, arrow);
            }
        }
    }
}
