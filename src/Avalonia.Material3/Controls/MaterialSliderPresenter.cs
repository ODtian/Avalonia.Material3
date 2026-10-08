using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Globalization;

namespace Avalonia.Material3.Controls;

/// <summary>Default Material slider template surface. Hosts track and handle visuals.</summary>
public sealed class MaterialSliderPresenter : Control
{
    private MaterialSlider? _owner;
    private readonly List<Control> _endpoints = [];
    internal IReadOnlyList<Control> Endpoints => _endpoints;

    internal void Attach(MaterialSlider? owner)
    {
        _owner = owner;
        VisualChildren.Clear(); LogicalChildren.Clear(); _endpoints.Clear();
        if (owner is MaterialRangeSlider range)
        {
            _endpoints.AddRange(range.EndpointControls);
            foreach (var endpoint in _endpoints) { VisualChildren.Add(endpoint); LogicalChildren.Add(endpoint); }
        }
        InvalidateMeasure(); InvalidateVisual();
    }

    private bool Vertical => _owner?.Orientation == Orientation.Vertical;
    private MaterialSliderGeometry Geometry => new(Vertical ? Bounds.Height : Bounds.Width, Vertical ? Bounds.Width : Bounds.Height);
    private double LabelSpace => _owner is { ValueLabelVisibility: not SliderValueLabelVisibility.Never } ? _owner.FontSize * 1.5 + 24 : 0;
    private double Position(double value) => Geometry.Thumb(_owner!.Fraction(value), _owner.Step > 0);
    private double PhysicalPosition(double value) => _owner!.ReverseDirection ? Geometry.Length - Position(value) : Position(value);
    private bool Focused(int index) => index == 0 && _owner!.IsFocused || _endpoints.Count > index && _endpoints[index].IsFocused;
    private double ThumbWidth(int index) => Focused(index) || _owner!.IsDragging && (index == 1) == _owner.ActiveUpper ? 2 : 4;

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var endpoint in _endpoints) endpoint.Measure(availableSize);
        return Vertical ? new Size(64 + LabelSpace, 48) : new Size(48, 64 + LabelSpace);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_owner is MaterialRangeSlider range)
        {
            var geometry = new MaterialSliderGeometry(Vertical ? finalSize.Height : finalSize.Width, Vertical ? finalSize.Width : finalSize.Height);
            for (var i = 0; i < 2; i++)
            {
                var position = geometry.Thumb(range.Fraction(i == 0 ? range.LowerValue : range.UpperValue), range.Step > 0);
                if (range.ReverseDirection) position = geometry.Length - position;
                var center = Vertical ? new Point(geometry.Axis, geometry.Length - position) : new Point(position, geometry.Axis);
                _endpoints[i].Arrange(new Rect(center.X - 24, center.Y - 24, 48, 48));
            }
        }
        return finalSize;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (_owner is not { } owner) return;
        context.DrawRectangle(Brushes.Transparent, null, new Rect(Bounds.Size));
        var transform = Vertical
            ? (owner.ReverseDirection ? new Matrix(0, 1, 1, 0, 0, 0) : new Matrix(0, -1, 1, 0, 0, Bounds.Height))
            : (owner.ReverseDirection ? new Matrix(-1, 0, 0, 1, Bounds.Width, 0) : Matrix.Identity);
        using (context.PushTransform(transform)) DrawTrack(context);
        var showLabel = owner.ValueLabelVisibility == SliderValueLabelVisibility.Always ||
            owner.ValueLabelVisibility == SliderValueLabelVisibility.OnInteraction && (owner.IsDragging || owner.IsKeyboardFocusWithin);
        if (!showLabel) return;
        var lower = Label(PhysicalPosition(owner.Value), owner.ValueText);
        if (owner is MaterialRangeSlider range)
        {
            var upper = Label(PhysicalPosition(range.UpperValue), owner.FormatValue(range.UpperValue));
            if (lower.Rect.Intersects(upper.Rect)) Separate(ref lower, ref upper);
            DrawLabel(context, lower); DrawLabel(context, upper);
        }
        else DrawLabel(context, lower);
    }

    private void DrawTrack(DrawingContext context)
    {
        var owner = _owner!;
        var geometry = Geometry;
        var lower = Position(owner.Value);
        var upper = owner is MaterialRangeSlider range ? Position(range.UpperValue) : lower;
        // ThumbContent keeps a4-DIP layout box while the focused/pressed ink narrows to2.
        // The track follows the layout box, retaining the same8-DIP exclusion in every state.
        var lowerGap = MaterialSliderGeometry.Gap(4);
        var upperGap = MaterialSliderGeometry.Gap(4);
        var center = (geometry.Start + geometry.End) / 2;
        var leadingEnd = lower - lowerGap;
        var trailingStart = upper + upperGap;
        var leadingStop = false;
        if (owner.CenteredTrack && owner is not MaterialRangeSlider)
        {
            leadingEnd = Math.Min(lower, center) - (lower < center ? lowerGap : 0);
            trailingStart = Math.Max(lower, center) + (lower >= center ? lowerGap : 0);
            DrawSegment(context, geometry.Start, leadingEnd, owner.Background, trailingHandle: lower <= center);
            DrawSegment(context, Math.Min(lower, center) + (lower < center ? lowerGap : 0),
                Math.Max(lower, center) - (lower > center ? lowerGap : 0), owner.Foreground, true, true, true);
            leadingStop = leadingEnd > geometry.Start + geometry.CapInset;
        }
        else
        {
            if (owner is MaterialRangeSlider)
            {
                DrawSegment(context, geometry.Start, leadingEnd, owner.Background, trailingHandle: true);
                leadingStop = leadingEnd > geometry.Start + geometry.CapInset;
            }
            DrawSegment(context, owner is MaterialRangeSlider ? lower + lowerGap : geometry.Start,
                upper - upperGap, owner.Foreground, true, owner is MaterialRangeSlider, true);
        }
        DrawSegment(context, trailingStart, geometry.End, owner.Background, leadingHandle: !(owner.CenteredTrack && lower < center));
        var trailingStop = trailingStart < geometry.End - geometry.CapInset;
        using var activeOpacity = context.PushOpacity(MaterialModalPaintScope.IsEnabledForPaint(owner) ? 1 : owner.DisabledActiveOpacity);
        if (owner.ShowMarks) DrawMarks(context, lower, upper, lowerGap, upperGap, leadingStop, trailingStop);
        if (leadingStop) context.DrawEllipse(owner.Foreground, null, new Point(geometry.Start + geometry.CapInset, geometry.Axis), 2, 2);
        if (trailingStop) context.DrawEllipse(owner.Foreground, null, new Point(geometry.End - geometry.CapInset, geometry.Axis), 2, 2);
        DrawHandle(context, lower, 0);
        if (owner is MaterialRangeSlider) DrawHandle(context, upper, 1);
    }

    private void DrawSegment(DrawingContext context, double start, double end, IBrush? brush, bool active = false,
        bool leadingHandle = false, bool trailingHandle = false)
    {
        if (end <= start) return;
        var owner = _owner!;
        using var opacity = context.PushOpacity(MaterialModalPaintScope.IsEnabledForPaint(owner) ? 1 : active ? owner.DisabledActiveOpacity : owner.DisabledInactiveOpacity);
        var leading = leadingHandle ? 2 : 8;
        var trailing = trailingHandle ? 2 : 8;
        context.DrawRectangle(brush, null, new RoundedRect(new Rect(start, Geometry.Axis - 8, end - start, 16),
            new CornerRadius(leading, trailing, trailing, leading)));
    }

    private void DrawMarks(DrawingContext context, double lower, double upper, double lowerGap, double upperGap, bool leadingStop, bool trailingStop)
    {
        var owner = _owner!;
        var geometry = Geometry;
        IEnumerable<double> marks = owner.Marks;
        if (owner.Marks.Count == 0 && owner.Step > 0 && owner.Maximum > owner.Minimum)
        {
            // Thin densely spaced visual ticks without changing the selectable values.
            var count = owner.StepIndex(owner.Maximum);
            var limit = Math.Clamp((int)Math.Min(4095, Math.Max(1, (geometry.End - geometry.Start) / 4)), 1, 4095);
            var ticks = new List<double> { owner.Minimum };
            if (double.IsFinite(count))
            {
                var stride = Math.Max(1, Math.Ceiling(count / limit));
                for (var i = 1; i <= limit && i * stride < count; i++) ticks.Add(owner.ValueAtFraction(i * stride / count));
            }
            ticks.Add(owner.Maximum); marks = ticks;
        }
        foreach (var value in marks.Where(value => value >= owner.Minimum && value <= owner.Maximum).Take(4096))
        {
            var fraction = owner.Fraction(value);
            if (fraction == 0 && leadingStop || fraction == 1 && trailingStop) continue;
            var x = geometry.Tick(fraction);
            if (Math.Abs(x - lower) <= lowerGap || owner is MaterialRangeSlider && Math.Abs(x - upper) <= upperGap) continue;
            var center = (geometry.Start + geometry.End) / 2;
            if (owner.CenteredTrack && owner is not MaterialRangeSlider && Math.Abs(x - center) <= lowerGap) continue;
            var selected = owner is MaterialRangeSlider ? x >= lower && x <= upper :
                owner.CenteredTrack ? x >= Math.Min(lower, center) && x <= Math.Max(lower, center) : x <= lower;
            context.DrawEllipse(selected ? owner.Background : owner.Foreground, null, new Point(x, geometry.Axis), 2, 2);
        }
    }

    private void DrawHandle(DrawingContext context, double x, int index)
    {
        var brush = _owner!.Foreground;
        var width = ThumbWidth(index);
        context.DrawRectangle(brush, null, new Rect(x - width / 2, Geometry.Axis - 22, width, 44), width / 2, width / 2);
    }

    private readonly record struct LabelLayout(FormattedText Text, Rect Rect);
    private LabelLayout Label(double handle, string value)
    {
        var owner = _owner!;
        var text = new FormattedText(value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(owner.FontFamily, owner.FontStyle, owner.FontWeight), owner.FontSize, owner.ValueIndicatorForeground);
        // MDC TooltipDrawable consumes the inherited 4-DIP padding and 28-DIP minimum width;
        // Slider.Label overrides the minimum height to 32. Its start/end padding is not consumed.
        var width = Math.Min(Bounds.Width, Math.Max(28, text.Width + 8));
        var height = Math.Max(32, owner.FontSize);
        var x = Math.Clamp(handle - width / 2, 0, Math.Max(0, Bounds.Width - width));
        var y = Geometry.Axis - 22 - 12 - height;
        if (Vertical)
        {
            x = Math.Max(0, Geometry.Axis - 12 - width);
            y = Math.Clamp(Geometry.Length - handle - height / 2, 0, Math.Max(0, Geometry.Length - height));
        }
        return new(text, new Rect(x, y, width, height));
    }

    private void Separate(ref LabelLayout lower, ref LabelLayout upper)
    {
        var first = Vertical ? lower.Rect.Center.Y <= upper.Rect.Center.Y : lower.Rect.Center.X <= upper.Rect.Center.X;
        var a = first ? lower : upper;
        var b = first ? upper : lower;
        var aSpan = Vertical ? a.Rect.Height : a.Rect.Width;
        var bSpan = Vertical ? b.Rect.Height : b.Rect.Width;
        var length = Vertical ? Bounds.Height : Bounds.Width;
        var midpoint = Vertical ? (a.Rect.Center.Y + b.Rect.Center.Y) / 2 : (a.Rect.Center.X + b.Rect.Center.X) / 2;
        var aStart = Math.Clamp(midpoint - aSpan - 1, 0, Math.Max(0, length - aSpan - bSpan - 2));
        var bStart = Math.Min(length - bSpan, aStart + aSpan + 2);
        a = a with { Rect = Vertical ? new Rect(a.Rect.X, aStart, a.Rect.Width, aSpan) : new Rect(aStart, a.Rect.Y, aSpan, a.Rect.Height) };
        b = b with { Rect = Vertical ? new Rect(b.Rect.X, bStart, b.Rect.Width, bSpan) : new Rect(bStart, b.Rect.Y, bSpan, b.Rect.Height) };
        if (first) { lower = a; upper = b; } else { lower = b; upper = a; }
    }

    private void DrawLabel(DrawingContext context, LabelLayout label)
    {
        var radius = Math.Min(label.Rect.Width, label.Rect.Height) / 2;
        context.DrawRectangle(_owner!.ValueIndicatorBrush, null, label.Rect, radius, radius);
        using (context.PushClip(label.Rect)) context.DrawText(label.Text, label.Rect.TopLeft +
            new Vector((label.Rect.Width - label.Text.Width) / 2, (label.Rect.Height - label.Text.Height) / 2));
    }
}
