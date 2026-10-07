using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

// DatePicker.Month uses six rows and SpaceEvenly 48-DIP targets. The range
// background has its own seven-column spacing formula in DateRangePicker.
internal sealed class MaterialCalendarMonthView : Panel
{
    private readonly MaterialDatePicker _owner;
    private readonly int _firstColumn;
    private double _cell = 48;
    internal double CellSize => _cell;
    internal event Action<double>? CellSizeChanged;
    internal DateOnly Month { get; }
    internal MaterialCalendarMonthView(MaterialDatePicker owner, DateOnly month)
    {
        _owner = owner; Month = month; UseLayoutRounding = false;
        _firstColumn = ((int)month.DayOfWeek - (int)owner.DateCulture.DateTimeFormat.FirstDayOfWeek + 7) % 7;
        Children.Add(new MaterialCalendarRangeBackground(this) { IsHitTestVisible = false });
        for (var i = 0; i < 42; i++)
        {
            var day = i - _firstColumn + 1;
            if (day < 1 || day > DateTime.DaysInMonth(month.Year, month.Month))
                Children.Add(new Border { MinWidth = 48, MinHeight = 48 });
            else
            {
                var date = month.AddDays(day - 1);
                var action = new MaterialCalendarDay { Date = date, Content = day.ToString(owner.Culture) };
                action.Click += (_, _) => owner.SelectDate(date);
                Children.Add(action);
            }
        }
        Refresh();
    }
    internal void Refresh()
    {
        foreach (var day in Children.OfType<MaterialCalendarDay>()) _owner.RefreshDay(day);
        Children[0].InvalidateVisual();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var oldSize = _cell;
        _cell = 48;
        foreach (var child in Children)
        {
            child.Measure(Size.Infinity);
            _cell = Math.Max(_cell, Math.Max(child.DesiredSize.Width, child.DesiredSize.Height));
        }
        if (_cell != oldSize) CellSizeChanged?.Invoke(_cell);
        return new(_cell * 7, _cell * 6);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var gap = Math.Max(0, (finalSize.Width - 7 * _cell) / 8);
        Children[0].Arrange(new Rect(finalSize));
        for (var i = 0; i < 42; i++)
            Children[i + 1].Arrange(new Rect(gap + i % 7 * (_cell + gap), i / 7 * _cell, _cell, _cell));
        return finalSize;
    }
    internal void PaintRange(DrawingContext context, IBrush? brush)
    {
        if (_owner.SelectionMode != MaterialDateSelectionMode.Range || _owner.SelectedDate is not { } start ||
            _owner.RangeEnd is not { } end || start >= end) return;
        var last = Month.AddDays(DateTime.DaysInMonth(Month.Year, Month.Month) - 1);
        if (end < Month || start > last) return;
        var firstDay = start < Month ? Month : start;
        var lastDay = end > last ? last : end;
        var firstIndex = _firstColumn + firstDay.Day - 1;
        var lastIndex = _firstColumn + lastDay.Day - 1;
        var firstRow = firstIndex / 7; var lastRow = lastIndex / 7;
        var space = (Bounds.Width - _cell * 7) / 7;
        var startX = firstIndex % 7 * (_cell + space) + (firstDay == start ? _cell / 2 : 0) + space / 2;
        var endX = lastIndex % 7 * (_cell + space) + (lastDay == end ? _cell / 2 : _cell) + space / 2;
        // Avalonia mirrors this inherited-flow visual together with its date
        // targets. Drawing in logical coordinates applies the source RTL mirror once.
        for (var row = firstRow; row <= lastRow; row++)
        {
            var left = row == firstRow ? startX : 0;
            var right = row == lastRow ? endX : Bounds.Width;
            context.DrawRectangle(brush, null, new Rect(Math.Min(left, right), row * _cell + (_cell - 40) / 2, Math.Abs(right - left), 40));
        }
    }
}

internal sealed class MaterialCalendarRangeBackground : Control
{
    private static readonly StyledProperty<IBrush?> BrushProperty = AvaloniaProperty.Register<MaterialCalendarRangeBackground, IBrush?>("Brush");
    private readonly MaterialCalendarMonthView _month;
    static MaterialCalendarRangeBackground() => AffectsRender<MaterialCalendarRangeBackground>(BrushProperty);
    internal MaterialCalendarRangeBackground(MaterialCalendarMonthView month)
    {
        _month = month;
        MaterialPickerSupport.Resource(this, BrushProperty, "SecondaryContainerBrush");
    }
    public override void Render(DrawingContext context) => _month.PaintRange(context, GetValue(BrushProperty));
}

internal sealed class MaterialCalendarWeekRow : Panel
{
    private double _minimumCell = 48;
    internal double MinimumCellSize
    {
        get => _minimumCell;
        set { if (_minimumCell == value) return; _minimumCell = value; InvalidateMeasure(); }
    }
    internal void Refresh(MaterialDatePicker owner)
    {
        Children.Clear();
        var format = owner.DateCulture.DateTimeFormat;
        for (var i = 0; i < 7; i++)
        {
            var day = (DayOfWeek)(((int)format.FirstDayOfWeek + i) % 7);
            var label = MaterialPickerSupport.Text("BodyLarge");
            label.MinWidth = label.MinHeight = 48; label.TextAlignment = TextAlignment.Center;
            label.Text = format.GetShortestDayName(day);
            Avalonia.Automation.AutomationProperties.SetName(label, format.GetDayName(day));
            Children.Add(label);
        }
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children) child.Measure(Size.Infinity);
        var cell = Children.Count == 0 ? _minimumCell : Math.Max(_minimumCell, Children.Max(c => Math.Max(c.DesiredSize.Width, c.DesiredSize.Height)));
        return new(cell * 7, cell);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var cell = DesiredSize.Height;
        var gap = Math.Max(0, (finalSize.Width - cell * 7) / 8);
        for (var i = 0; i < Children.Count; i++) Children[i].Arrange(new Rect(gap + i * (cell + gap), 0, cell, cell));
        return finalSize;
    }
}
