using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

// Template implementation, not a public extension seam.
internal sealed class MaterialProgressPresenter : Control
{
    public static readonly StyledProperty<MaterialProgressIndicator?> IndicatorProperty =
        AvaloniaProperty.Register<MaterialProgressPresenter, MaterialProgressIndicator?>(nameof(Indicator));
    private bool _subscribed;
    public MaterialProgressIndicator? Indicator { get => GetValue(IndicatorProperty); set => SetValue(IndicatorProperty, value); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IndicatorProperty)
        {
            if (_subscribed && change.OldValue is MaterialProgressIndicator old) old.FrameChanged -= InvalidateVisual;
            _subscribed = false;
            if (VisualRoot is not null) Subscribe();
            InvalidateVisual();
        }
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
    public override void Render(DrawingContext context)
    {
        if (Indicator is not { } indicator || Bounds.Width <= 0 || Bounds.Height <= 0) return;
        if (indicator.IsTerminal)
        {
            var failed = indicator.Status == MaterialProgressStatus.Failed;
            var pen = new Pen(failed ? indicator.ErrorBrush : indicator.Foreground, 2, lineCap: PenLineCap.Round);
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
        if (indicator.IsContained)
            context.DrawRectangle(indicator.ContainerBrush, null, new RoundedRect(new Rect(Bounds.Size), indicator.ContainerCornerRadius));
        Vector[] from;
        Vector[] to;
        double morph;
        double rotation;
        if (indicator.EffectiveIndeterminate)
        {
            var cycle = indicator.Elapsed / .65;
            var index = (int)(Math.Floor(cycle) % 7);
            var time = (cycle - Math.Floor(cycle)) * .65;
            morph = indicator.ReducedMotion ? 0 : MaterialSpringResponse.Evaluate(time, indicator.MotionSpring);
            rotation = (Math.Floor(cycle) % 4 + 1 + morph) * Math.PI / 2 + (indicator.Elapsed % 4.666) / 4.666 * Math.Tau;
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
        var path = new StreamGeometry();
        var scale = Math.Min(38, Math.Min(Bounds.Width, Bounds.Height) * 38 / 48) / 2;
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        using (var drawing = path.Open())
        {
            for (var i = 0; i < from.Length; i++)
            {
                var vertex = from[i] + (to[i] - from[i]) * Math.Clamp(morph, 0, 1);
                var rotated = new Vector(vertex.X * Math.Cos(rotation) - vertex.Y * Math.Sin(rotation), vertex.X * Math.Sin(rotation) + vertex.Y * Math.Cos(rotation));
                var point = center + rotated * scale;
                if (i == 0) drawing.BeginFigure(point, true); else drawing.LineTo(point);
            }
            drawing.EndFigure(true);
        }
        context.DrawGeometry(indicator.IsContained ? indicator.ContainedForeground : indicator.Foreground, null, path);
    }

    private void DrawLinear(DrawingContext context, MaterialProgressIndicator indicator)
    {
        var width = Bounds.Width;
        var y = Bounds.Height / 2;
        if (width < 4) return;
        void Line(double start, double end, IBrush? brush, bool active = false)
        {
            if (end <= start || brush is null) return;
            start = Math.Clamp(start, 2, width - 2);
            end = Math.Clamp(end, 2, width - 2);
            var rtl = indicator.FlowDirection == FlowDirection.RightToLeft;
            Point Position(double x) => new(rtl ? width - x : x, y + (active && indicator.IsExpressive
                ? 3 * indicator.WaveAmplitude * Math.Sin(Math.Tau * (x / (indicator.EffectiveIndeterminate ? 20 : 40) - indicator.Elapsed)) : 0));
            var path = new StreamGeometry();
            using (var drawing = path.Open())
            {
                drawing.BeginFigure(Position(start), false);
                var count = Math.Max(1, (int)Math.Ceiling(end - start));
                for (var i = 1; i <= count; i++) drawing.LineTo(Position(start + (end - start) * i / count));
                drawing.EndFigure(false);
            }
            context.DrawGeometry(null, new Pen(brush, 4, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round), path);
        }
        if (!indicator.EffectiveIndeterminate)
        {
            var progress = indicator.EffectiveValue;
            Line(progress * width + Math.Min(progress * width, 8), width, indicator.TrackBrush);
            Line(0, progress * width, indicator.Foreground, true);
            context.DrawEllipse(indicator.Foreground, null, new Point(indicator.FlowDirection == FlowDirection.RightToLeft ? 2 : width - 2, y), 2, 2);
            return;
        }
        var t = indicator.Elapsed * 1000 % 1750;
        var easing = indicator.AccelerateEasing;
        double Position(double delay, double duration) => easing.Ease(Math.Clamp((t - delay) / duration, 0, 1)) * width;
        var head1 = Position(0, 1000);
        var tail1 = Position(250, 1000);
        var head2 = Position(650, 850);
        var tail2 = Position(900, 850);
        if (indicator.ReducedMotion) { tail1 = width * .1; head1 = width * .45; tail2 = head2 = 0; }
        Line(head1 > 0 ? head1 + 8 : 0, width, indicator.TrackBrush);
        Line(head2 > 0 ? head2 + 8 : 0, tail1 < width ? tail1 - 8 : width, indicator.TrackBrush);
        Line(0, tail2 < width ? tail2 - 8 : width, indicator.TrackBrush);
        Line(tail1, head1, indicator.Foreground, true);
        Line(tail2, head2, indicator.Foreground, true);
    }

    private void DrawCircular(DrawingContext context, MaterialProgressIndicator indicator)
    {
        var diameter = Math.Min(Bounds.Width, Bounds.Height);
        var radius = (diameter - 4) / 2 - (indicator.IsExpressive ? 1.6 : 0);
        if (radius <= 0) return;
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var progress = indicator.EffectiveValue;
        var start = -.25;
        if (indicator.EffectiveIndeterminate)
        {
            var t = indicator.Elapsed % 6;
            var half = t < 3 ? t / 3 : (6 - t) / 3;
            progress = .1 + .77 * indicator.ProgressEasing.Ease(half);
            var segment = Math.Floor(t / 1.5);
            var within = (t % 1.5) / .3;
            start = t / 6 * 3 + (segment + indicator.DecelerateEasing.Ease(Math.Clamp(within, 0, 1))) / 4;
        }
        var gap = Math.Min(progress, 8 / (Math.PI * diameter));
        if (!indicator.EffectiveIndeterminate || indicator.IsExpressive)
            Arc(context, center, radius, start + progress + gap, Math.Max(0, 1 - progress - 2 * gap), indicator.TrackBrush);
        Arc(context, center, radius, start, progress, indicator.Foreground, indicator.IsExpressive ? indicator.WaveAmplitude * 1.6 : 0, indicator.Elapsed);
    }

    private static void Arc(DrawingContext context, Point center, double radius, double start, double sweep, IBrush? brush, double amplitude = 0, double phase = 0)
    {
        if (sweep <= 0 || brush is null) return;
        var path = new StreamGeometry();
        using (var drawing = path.Open())
        {
            var count = Math.Max(2, (int)Math.Ceiling(sweep * 256));
            for (var i = 0; i <= count; i++)
            {
                var angle = (start + sweep * i / count) * Math.Tau;
                var waves = Math.Max(1, (int)Math.Round(Math.Tau * radius / 15));
                var waveRadius = radius + amplitude * Math.Sin(angle * waves - phase * Math.Tau);
                var point = new Point(center.X + waveRadius * Math.Cos(angle), center.Y + waveRadius * Math.Sin(angle));
                if (i == 0) drawing.BeginFigure(point, false); else drawing.LineTo(point);
            }
            drawing.EndFigure(false);
        }
        context.DrawGeometry(null, new Pen(brush, 4, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round), path);
    }
}
