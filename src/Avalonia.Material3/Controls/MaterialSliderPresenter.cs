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
        VisualChildren.Clear();
        LogicalChildren.Clear();
        _endpoints.Clear();
        if (owner is MaterialRangeSlider range)
        {
            _endpoints.AddRange(range.EndpointControls);
            foreach (var endpoint in _endpoints)
            {
                VisualChildren.Add(endpoint);
                LogicalChildren.Add(endpoint);
            }
        }
        InvalidateMeasure();
        InvalidateVisual();
    }

    private bool Vertical => _owner?.Orientation == Orientation.Vertical;
    private double Length => Vertical ? Bounds.Height : Bounds.Width;
    private double Breadth => Vertical ? Bounds.Width : Bounds.Height;
    private double LabelSpace => _owner is { ValueLabelVisibility: not SliderValueLabelVisibility.Never } ? _owner.FontSize * 1.5 + 24 : 0;
    private double TrackY => Breadth - 32;
    private double Position(double value) => 24 + _owner!.Fraction(value) * Math.Max(0, Length - 48);
    private double PhysicalPosition(double value) => _owner!.ReverseDirection ? Length - Position(value) : Position(value);

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var endpoint in _endpoints) endpoint.Measure(availableSize);
        return Vertical ? new Size(64 + LabelSpace, 48) : new Size(48, 64 + LabelSpace);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_owner is MaterialRangeSlider range)
        {
            var length = Vertical ? finalSize.Height : finalSize.Width;
            var breadth = Vertical ? finalSize.Width : finalSize.Height;
            for (var i = 0; i < 2; i++)
            {
                var position = 24 + range.Fraction(i == 0 ? range.LowerValue : range.UpperValue) * Math.Max(0, length - 48);
                if (range.ReverseDirection) position = length - position;
                var center = Vertical ? new Point(breadth - 32, length - position) : new Point(position, breadth - 32);
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
            (owner.ValueLabelVisibility == SliderValueLabelVisibility.OnInteraction && (owner.IsDragging || owner.IsKeyboardFocusWithin));
        if (!showLabel) return;
        if (owner is MaterialRangeSlider range)
        {
            DrawLabel(context, PhysicalPosition(range.LowerValue), range.ValueText, false);
            DrawLabel(context, PhysicalPosition(range.UpperValue), owner.FormatValue(range.UpperValue), true);
        }
        else DrawLabel(context, PhysicalPosition(owner.Value), owner.ValueText, null);
    }

    private void DrawTrack(DrawingContext context)
    {
        var owner = _owner!;
        var start = 24d;
        var end = Math.Max(start, Length - 24);
        var lower = Position(owner.Value);
        var upper = owner is MaterialRangeSlider range ? Position(range.UpperValue) : lower;
        if (owner.CenteredTrack && owner is not MaterialRangeSlider)
        {
            var center = (start + end) / 2;
            if (lower < center)
            {
                DrawSegment(context, start, lower - 8, owner.Background, trailingHandle: true);
                DrawSegment(context, lower + 8, center, owner.Foreground, true, leadingHandle: true);
                DrawSegment(context, center, end, owner.Background);
            }
            else
            {
                DrawSegment(context, start, Math.Min(center, lower - 8), owner.Background, trailingHandle: lower == center);
                DrawSegment(context, center, lower - 8, owner.Foreground, true, trailingHandle: true);
                DrawSegment(context, lower + 8, end, owner.Background, leadingHandle: true);
            }
        }
        else
        {
            if (owner is MaterialRangeSlider) DrawSegment(context, start, lower - 8, owner.Background, trailingHandle: true);
            DrawSegment(context, owner is MaterialRangeSlider ? lower + 8 : start, upper - 8, owner.Foreground, true,
                leadingHandle: owner is MaterialRangeSlider, trailingHandle: true);
            DrawSegment(context, upper + 8, end, owner.Background, leadingHandle: true);
        }
        using var activeOpacity = context.PushOpacity(owner.IsEffectivelyEnabled ? 1 : owner.DisabledActiveOpacity);
        if (owner.ShowMarks) DrawMarks(context, start, end, lower, upper);
        DrawHandle(context, lower, owner.IsFocused || (_endpoints.Count > 0 && _endpoints[0].IsFocused), !owner.ActiveUpper);
        if (owner is MaterialRangeSlider) DrawHandle(context, upper, _endpoints[1].IsFocused, owner.ActiveUpper);
        if (owner.IsEffectivelyEnabled && owner.IsPointerOver && !owner.IsDragging)
            context.DrawEllipse(null, new Pen(owner.Foreground, 1), new Point(owner.ActiveUpper ? upper : lower, TrackY), 12, 24);
        context.DrawEllipse(owner.Foreground, null, new Point(end, TrackY), 2, 2);
    }

    private void DrawSegment(DrawingContext context, double start, double end, IBrush? brush, bool active = false,
        bool leadingHandle = false, bool trailingHandle = false)
    {
        if (end <= start) return;
        using var opacity = context.PushOpacity(_owner!.IsEffectivelyEnabled ? 1 :
            active ? _owner.DisabledActiveOpacity : _owner.DisabledInactiveOpacity);
        var leading = leadingHandle ? 2 : 8;
        var trailing = trailingHandle ? 2 : 8;
        var rect = new RoundedRect(new Rect(start, TrackY - 8, end - start, 16), new CornerRadius(leading, trailing, trailing, leading));
        context.DrawRectangle(brush, null, rect);
    }

    private void DrawMarks(DrawingContext context, double start, double end, double lower, double upper)
    {
        var owner = _owner!;
        IEnumerable<double> marks = owner.Marks;
        if (owner.Marks.Count == 0 && owner.Step > 0 && owner.Maximum > owner.Minimum)
        {
            // Thin densely spaced visual ticks without changing the set of selectable values.
            var count = owner.StepIndex(owner.Maximum);
            var limit = Math.Clamp((int)Math.Min(4095, Math.Max(1, (end - start) / 4)), 1, 4095);
            var ticks = new List<double> { owner.Minimum };
            if (double.IsFinite(count))
            {
                var stride = Math.Max(1, Math.Ceiling(count / limit));
                for (var i = 1; i <= limit && i * stride < count; i++)
                    ticks.Add(owner.ValueAtFraction(i * stride / count));
            }
            ticks.Add(owner.Maximum);
            marks = ticks;
        }
        foreach (var value in marks.Where(value => value >= owner.Minimum && value <= owner.Maximum).Take(4096))
        {
            var x = Position(value);
            if (Math.Abs(x - lower) < 8 || (owner is MaterialRangeSlider && Math.Abs(x - upper) < 8)) continue;
            var selected = owner is MaterialRangeSlider ? x >= lower && x <= upper :
                owner.CenteredTrack ? x >= Math.Min(lower, (start + end) / 2) && x <= Math.Max(lower, (start + end) / 2) : x <= lower;
            context.DrawEllipse(selected ? owner.ValueIndicatorForeground : owner.Foreground, null, new Point(x, TrackY), 2, 2);
        }
    }

    private void DrawHandle(DrawingContext context, double x, bool focused, bool active)
    {
        var brush = _owner!.Foreground;
        var width = (_owner.IsDragging && active) || focused ? 2 : 4;
        context.DrawRectangle(brush, null, new Rect(x - width / 2d, TrackY - 22, width, 44), 2, 2);
        if (focused && _owner.IsEffectivelyEnabled)
            context.DrawRectangle(null, new Pen(brush, 3), new Rect(x - 9, TrackY - 27, 18, 54), 9, 9);
    }

    private void DrawLabel(DrawingContext context, double handle, string value, bool? upper)
    {
        var owner = _owner!;
        var text = new FormattedText(value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(owner.FontFamily, owner.FontStyle, owner.FontWeight), owner.FontSize, owner.ValueIndicatorForeground);
        var width = Math.Min(Bounds.Width, text.Width + 16);
        var height = text.Height + 12;
        var x = Math.Clamp(handle - width / 2, 0, Math.Max(0, Bounds.Width - width));
        var y = TrackY - 22 - 12 - height;
        if (Vertical)
        {
            x = Math.Max(0, TrackY - 12 - width);
            y = Math.Clamp(Length - handle - height / 2, 0, Math.Max(0, Length - height));
        }
        else if (upper.HasValue)
        {
            // Separate coincident endpoint labels instead of drawing one on top of the other.
            var trailing = upper.Value ^ owner.ReverseDirection;
            x = trailing ? Math.Max(x, Bounds.Width / 2) : Math.Min(x, Math.Max(0, Bounds.Width / 2 - width));
            x = Math.Clamp(x, 0, Math.Max(0, Bounds.Width - width));
        }
        var rect = new Rect(x, y, width, height);
        context.DrawRectangle(owner.ValueIndicatorBrush, null, rect, height / 2, height / 2);
        using (context.PushClip(rect)) context.DrawText(text, new Point(x + 8, y + 6));
    }
}
