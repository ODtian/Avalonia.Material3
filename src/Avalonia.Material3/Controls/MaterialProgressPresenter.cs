using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

// Private paint projection. Pens and the last path per role are bounded, not a time-keyed global cache.
internal sealed class MaterialProgressPresenter : Control
{
    public static readonly StyledProperty<MaterialProgressIndicator?> IndicatorProperty =
        AvaloniaProperty.Register<MaterialProgressPresenter, MaterialProgressIndicator?>(nameof(Indicator));
    private bool _subscribed;
    private Pen? _activePen, _trackPen, _resultPen;
    private readonly CachedPath _activeArc = new(), _trackArc = new(), _firstWave = new(), _secondWave = new();
    private StreamGeometry? _loadingPath;
    private Vector[]? _loadingFrom, _loadingTo;
    private double _loadingMorph;
    public MaterialProgressIndicator? Indicator { get => GetValue(IndicatorProperty); set => SetValue(IndicatorProperty, value); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != IndicatorProperty) return;
        if (_subscribed && change.OldValue is MaterialProgressIndicator old) old.FrameChanged -= InvalidateVisual;
        _subscribed = false;
        if (VisualRoot is not null) Subscribe();
        InvalidateVisual();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs args)
    {
        base.OnAttachedToVisualTree(args);
        Subscribe();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs args)
    {
        if (_subscribed && Indicator is { } indicator) indicator.FrameChanged -= InvalidateVisual;
        _subscribed = false;
        base.OnDetachedFromVisualTree(args);
    }
    private void Subscribe()
    {
        if (_subscribed || Indicator is not { } indicator) return;
        indicator.FrameChanged += InvalidateVisual;
        _subscribed = true;
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = Indicator?.GraphicWidth ?? double.NaN;
        if (!double.IsFinite(width)) width = double.IsFinite(availableSize.Width) ? availableSize.Width : 240;
        return new Size(width, Indicator?.GraphicHeight ?? 4);
    }
    private static Pen Stroke(ref Pen? pen, IBrush? brush, double thickness)
    {
        if (pen is null || pen.Brush != brush || pen.Thickness != thickness)
            pen = new Pen(brush, thickness, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        return pen;
    }
    public override void Render(DrawingContext context)
    {
        if (Indicator is not { } indicator || Bounds.Width <= 0 || Bounds.Height <= 0) return;
        if (indicator.IsTerminal)
        {
            var failed = indicator.Status == MaterialProgressStatus.Failed;
            var pen = Stroke(ref _resultPen, failed ? indicator.ErrorBrush : indicator.Foreground, 2);
            if (failed)
            {
                context.DrawLine(pen, new Point(4, 6), new Point(16, 18));
                context.DrawLine(pen, new Point(16, 6), new Point(4, 18));
            }
            else
            {
                context.DrawLine(pen, new Point(2, 12), new Point(7, 17));
                context.DrawLine(pen, new Point(7, 17), new Point(18, 6));
            }
        }
        else if (indicator is MaterialLoadingIndicator loading) DrawLoading(context, loading);
        else if (indicator is MaterialCircularProgressIndicator) DrawCircular(context, indicator);
        else DrawLinear(context, indicator);
    }
    private void DrawLoading(DrawingContext context, MaterialLoadingIndicator indicator)
    {
        var elapsed = indicator.Elapsed;
        if (indicator.IsContained)
            context.DrawRectangle(indicator.ContainerBrush, null, new RoundedRect(new Rect(Bounds.Size), indicator.ContainerCornerRadius));
        Vector[] from, to;
        double morph, rotation;
        if (indicator.EffectiveIndeterminate)
        {
            var cycle = elapsed / .65;
            var whole = Math.Floor(cycle);
            var index = (int)(whole % 7);
            morph = indicator.ReducedMotion ? 0 : MaterialSpringResponse.Evaluate((cycle - whole) * .65, indicator.MotionSpring);
            rotation = (whole % 4 + 1 + morph) * Math.PI / 2 + (elapsed % 4.666) / 4.666 * Math.Tau;
            from = MaterialLoadingShapes.Cycle[index];
            to = MaterialLoadingShapes.Cycle[(index + 1) % 7];
        }
        else
        {
            morph = indicator.EffectiveValue;
            rotation = -morph * Math.PI;
            from = MaterialLoadingShapes.Circle;
            to = MaterialLoadingShapes.Cycle[0];
        }
        morph = Math.Clamp(morph, 0, 1);
        if (_loadingPath is null || _loadingFrom != from || _loadingTo != to || _loadingMorph != morph)
        {
            var path = new StreamGeometry();
            using (var drawing = path.Open())
            {
                for (var i = 0; i < from.Length; i++)
                {
                    var vertex = from[i] + (to[i] - from[i]) * morph;
                    var point = new Point(vertex.X, vertex.Y);
                    if (i == 0) drawing.BeginFigure(point, true); else drawing.LineTo(point);
                }
                drawing.EndFigure(true);
            }
            _loadingPath = path;
            _loadingFrom = from; _loadingTo = to; _loadingMorph = morph;
        }
        // Rotation/scale do not require resampling the unit silhouette or 960 repeated trig calls.
        var scale = Math.Min(38, Math.Min(Bounds.Width, Bounds.Height) * 38 / 48) / 2;
        using (context.PushTransform(Matrix.CreateRotation(rotation) * Matrix.CreateScale(scale, scale)
            * Matrix.CreateTranslation(Bounds.Width / 2, Bounds.Height / 2)))
            context.DrawGeometry(indicator.IsContained ? indicator.ContainedForeground : indicator.Foreground, null, _loadingPath);
    }
    private void DrawLinear(DrawingContext context, MaterialProgressIndicator indicator)
    {
        var width = Bounds.Width;
        var y = Bounds.Height / 2;
        if (width < 4) return;
        var elapsed = indicator.Elapsed;
        var unknown = indicator.EffectiveIndeterminate;
        var amplitude = indicator.IsExpressive ? 3 * indicator.WaveAmplitude : 0;
        var wavelength = unknown ? 20 : 40;
        var activePen = Stroke(ref _activePen, indicator.Foreground, 4);
        var trackPen = Stroke(ref _trackPen, indicator.TrackBrush, 4);
        void Line(double start, double end, bool active = false, bool second = false)
        {
            var pen = active ? activePen : trackPen;
            if (end <= start || pen.Brush is null) return;
            start = Math.Clamp(start, 2, width - 2);
            end = Math.Clamp(end, 2, width - 2);
            if (!active || amplitude == 0)
            {
                context.DrawLine(pen, new Point(start, y), new Point(end, y));
                return;
            }
            var cache = second ? _secondWave : _firstWave;
            var key = new PathKey(Bounds.Size, start, end, amplitude, elapsed);
            if (cache.Geometry is null || cache.Key != key)
            {
                var path = new StreamGeometry();
                using (var drawing = path.Open())
                {
                    Point Position(double x) => new(x, y + amplitude * Math.Sin(Math.Tau * (x / wavelength - elapsed)));
                    drawing.BeginFigure(Position(start), false);
                    var count = Math.Max(1, (int)Math.Min(2048, Math.Ceiling(end - start)));
                    for (var i = 1; i <= count; i++) drawing.LineTo(Position(start + (end - start) * i / count));
                    drawing.EndFigure(false);
                }
                cache.Geometry = path; cache.Key = key;
            }
            context.DrawGeometry(null, pen, cache.Geometry);
        }
        if (!unknown)
        {
            var progress = indicator.EffectiveValue;
            Line(progress * width + Math.Min(progress * width, 8), width);
            Line(0, progress * width, true);
            context.DrawEllipse(indicator.Foreground, null, new Point(width - 2, y), 2, 2);
            return;
        }
        var t = elapsed * 1000 % 1750;
        var easing = indicator.AccelerateEasing;
        double Position(double delay, double duration) => easing.Ease(Math.Clamp((t - delay) / duration, 0, 1)) * width;
        var head1 = Position(0, 1000); var tail1 = Position(250, 1000);
        var head2 = Position(650, 850); var tail2 = Position(900, 850);
        if (indicator.ReducedMotion) { tail1 = width * .1; head1 = width * .45; tail2 = head2 = 0; }
        Line(head1 > 0 ? head1 + 8 : 0, width);
        Line(head2 > 0 ? head2 + 8 : 0, tail1 < width ? tail1 - 8 : width);
        Line(0, tail2 < width ? tail2 - 8 : width);
        Line(tail1, head1, true);
        Line(tail2, head2, true, true);
    }
    private void DrawCircular(DrawingContext context, MaterialProgressIndicator indicator)
    {
        var diameter = Math.Min(Bounds.Width, Bounds.Height);
        var expressive = indicator.IsExpressive;
        var radius = (diameter - 4) / 2 - (expressive ? 1.6 : 0);
        if (radius <= 0) return;
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var progress = indicator.EffectiveValue;
        var elapsed = indicator.Elapsed;
        var start = -.25;
        var unknown = indicator.EffectiveIndeterminate;
        if (unknown)
        {
            var t = elapsed % 6;
            var half = t < 3 ? t / 3 : (6 - t) / 3;
            progress = .1 + .77 * indicator.ProgressEasing.Ease(half);
            start = t / 6 * 3 + (Math.Floor(t / 1.5) + indicator.DecelerateEasing.Ease(Math.Clamp((t % 1.5) / .3, 0, 1))) / 4;
        }
        var gap = Math.Min(progress, 8 / (Math.PI * diameter));
        if (!unknown || expressive)
            Arc(context, _trackArc, Stroke(ref _trackPen, indicator.TrackBrush, 4), center, radius,
                start + progress + gap, Math.Max(0, 1 - progress - 2 * gap));
        Arc(context, _activeArc, Stroke(ref _activePen, indicator.Foreground, 4), center, radius, start, progress,
            expressive ? indicator.WaveAmplitude * 1.6 : 0, elapsed);
    }
    private static void Arc(DrawingContext context, CachedPath cache, Pen pen, Point center, double radius,
        double start, double sweep, double amplitude = 0, double phase = 0)
    {
        if (sweep <= 0 || pen.Brush is null) return;
        var key = new PathKey(new Size(center.X, center.Y), start, sweep, amplitude, amplitude == 0 ? 0 : phase, radius);
        if (cache.Geometry is null || cache.Key != key)
        {
            var path = new StreamGeometry();
            using (var drawing = path.Open())
            {
                Point Position(double turn) => new(center.X + radius * Math.Cos(turn * Math.Tau), center.Y + radius * Math.Sin(turn * Math.Tau));
                if (amplitude == 0)
                {
                    drawing.BeginFigure(Position(start), false);
                    if (sweep >= 1)
                    {
                        drawing.ArcTo(Position(start + .5), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
                        drawing.ArcTo(Position(start + 1), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
                    }
                    else drawing.ArcTo(Position(start + sweep), new Size(radius, radius), 0, sweep > .5, SweepDirection.Clockwise);
                }
                else
                {
                    var count = Math.Max(2, (int)Math.Ceiling(sweep * 256));
                    var waves = Math.Max(1, (int)Math.Round(Math.Tau * radius / 15));
                    var phaseAngle = phase * Math.Tau;
                    for (var i = 0; i <= count; i++)
                    {
                        var angle = (start + sweep * i / count) * Math.Tau;
                        var waveRadius = radius + amplitude * Math.Sin(angle * waves - phaseAngle);
                        var point = new Point(center.X + waveRadius * Math.Cos(angle), center.Y + waveRadius * Math.Sin(angle));
                        if (i == 0) drawing.BeginFigure(point, false); else drawing.LineTo(point);
                    }
                }
                drawing.EndFigure(false);
            }
            cache.Geometry = path; cache.Key = key;
        }
        context.DrawGeometry(null, pen, cache.Geometry);
    }
    private readonly record struct PathKey(Size Size, double First, double Second, double Amplitude, double Phase, double Radius = 0);
    private sealed class CachedPath
    {
        internal PathKey Key;
        internal StreamGeometry? Geometry;
    }
}
