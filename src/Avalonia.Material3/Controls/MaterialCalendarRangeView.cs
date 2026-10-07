using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.Material3.Controls;

// A physical scroll extent keeps native wheel/touch semantics, while only the
// viewport and one neighboring month on each side own calendar controls.
internal sealed class MaterialCalendarRangeView : Panel
{
    private readonly MaterialDatePicker _owner;
    private readonly MaterialCalendarWeekRow _week = new();
    private readonly MaterialCalendarMonthsPanel _months;
    private readonly ScrollViewer _scroll;
    private DateOnly? _displayed;
    private bool _positioning;
    private double? _resizedOffset;
    internal MaterialCalendarRangeView(MaterialDatePicker owner)
    {
        _owner = owner;
        _months = new(owner);
        _scroll = new ScrollViewer { Content = _months, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Margin = new Thickness(12, 0); Children.Add(_week); Children.Add(_scroll);
        _months.ItemHeightChanged += (oldHeight, newHeight) =>
        {
            _resizedOffset = (_resizedOffset ?? _scroll.Offset.Y) / oldHeight * newHeight;
            InvalidateMeasure();
        };
        _scroll.ScrollChanged += (_, _) =>
        {
            if (_resizedOffset is not null) return;
            _months.SetViewport(_scroll.Offset.Y, _scroll.Viewport.Height);
            if (_positioning || _months.Count == 0) return;
            _displayed = _months.MonthAt((int)(_scroll.Offset.Y / _months.ItemHeight));
            _owner.SetCurrentValue(MaterialDatePicker.DisplayMonthProperty, _displayed.Value);
        };
    }
    internal void Refresh(bool rebuild)
    {
        if (rebuild) _week.Refresh(_owner);
        var rebuilt = _months.Refresh(rebuild);
        var month = new DateOnly(_owner.DisplayMonth.Year, _owner.DisplayMonth.Month, 1);
        if (_displayed != month || rebuilt)
        {
            _displayed = month; _positioning = true;
            try
            {
                var offset = _months.IndexOf(month) * _months.ItemHeight;
                _months.SetViewport(offset, Math.Max(0, _scroll.Viewport.Height));
                _scroll.Offset = new Vector(0, offset);
            }
            finally { _positioning = false; }
        }
        InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        _months.SetViewport(_resizedOffset ?? _scroll.Offset.Y, double.IsFinite(availableSize.Height) ? availableSize.Height : _months.ItemHeight);
        _months.Measure(Size.Infinity);
        _week.MinimumCellSize = _months.CellSize;
        _week.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        var height = double.IsFinite(availableSize.Height) ? availableSize.Height : _week.DesiredSize.Height + _months.ItemHeight;
        var viewportHeight = Math.Max(0, height - _week.DesiredSize.Height);
        _months.SetViewport(_resizedOffset ?? _scroll.Offset.Y, viewportHeight);
        _scroll.Measure(new Size(availableSize.Width, viewportHeight));
        if (_week.MinimumCellSize != _months.CellSize)
        {
            _week.MinimumCellSize = _months.CellSize;
            _week.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            _scroll.Measure(new Size(availableSize.Width, Math.Max(0, height - _week.DesiredSize.Height)));
        }
        if (_resizedOffset is { } offset)
        {
            _resizedOffset = null; _positioning = true;
            try { _scroll.Offset = new Vector(0, offset); }
            finally { _positioning = false; }
        }
        return new(Math.Max(_week.DesiredSize.Width, _scroll.DesiredSize.Width), height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        _week.Arrange(new Rect(0, 0, finalSize.Width, _week.DesiredSize.Height));
        _scroll.Arrange(new Rect(0, _week.DesiredSize.Height, finalSize.Width, Math.Max(0, finalSize.Height - _week.DesiredSize.Height)));
        return finalSize;
    }
    internal bool FocusDate(DateOnly date) => _months.Children.OfType<StackPanel>()
        .SelectMany(p => p.Children.OfType<MaterialCalendarMonthView>())
        .SelectMany(p => p.Children.OfType<MaterialCalendarDay>()).FirstOrDefault(d => d.Date == date)?.Focus() ?? false;
}

internal sealed class MaterialCalendarMonthsPanel : Panel
{
    private readonly MaterialDatePicker _owner;
    private readonly SortedDictionary<int, StackPanel> _realized = new();
    private int _first;
    private double _offset, _viewport = 500;
    internal int Count { get; private set; }
    internal double ItemHeight { get; private set; } = 336;
    internal double CellSize { get; private set; } = 48;
    internal event Action<double, double>? ItemHeightChanged;
    internal MaterialCalendarMonthsPanel(MaterialDatePicker owner) { _owner = owner; UseLayoutRounding = false; }
    internal DateOnly MonthAt(int index)
    {
        var month = _first + Math.Clamp(index, 0, Math.Max(0, Count - 1));
        return new DateOnly(month / 12 + 1, month % 12 + 1, 1);
    }
    internal int IndexOf(DateOnly month) => Math.Clamp((month.Year - 1) * 12 + month.Month - 1 - _first, 0, Math.Max(0, Count - 1));
    internal bool Refresh(bool rebuild)
    {
        var first = (_owner.MinimumDate.Year - 1) * 12 + _owner.MinimumDate.Month - 1;
        var count = _owner.MinimumDate > _owner.MaximumDate ? 0 :
            (_owner.MaximumDate.Year - 1) * 12 + _owner.MaximumDate.Month - first;
        var changed = rebuild || _first != first || Count != count;
        if (changed)
        { _first = first; Count = count; Children.Clear(); _realized.Clear(); }
        Realize();
        foreach (var item in _realized.Values)
            ((MaterialCalendarMonthView)item.Children[1]).Refresh();
        InvalidateMeasure();
        return changed;
    }
    internal void SetViewport(double offset, double height)
    {
        if (_offset == offset && _viewport == height && _realized.Count > 0) return;
        _offset = Math.Max(0, offset); _viewport = Math.Max(48, height);
        Realize(); InvalidateMeasure();
    }
    private void Realize()
    {
        if (Count == 0) return;
        var first = Math.Min(Count - 1, Math.Max(0, (int)(_offset / ItemHeight) - 1));
        var last = Math.Min(Count - 1, (int)((_offset + _viewport) / ItemHeight) + 1);
        foreach (var index in _realized.Keys.Where(i => i < first || i > last).ToArray())
        { Children.Remove(_realized[index]); _realized.Remove(index); }
        for (var index = first; index <= last; index++)
        {
            if (_realized.ContainsKey(index)) continue;
            var month = MonthAt(index);
            var title = MaterialPickerSupport.Text("TitleSmall", "OnSurfaceVariant");
            title.Text = month.ToString(_owner.DateCulture.DateTimeFormat.YearMonthPattern, _owner.DateCulture);
            title.Margin = new Thickness(24, 20, 0, 8);
            var item = new StackPanel { Children = { title, new MaterialCalendarMonthView(_owner, month) } };
            _realized.Add(index, item); Children.Add(item);
        }
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 336d; var height = 0d;
        foreach (var item in _realized.Values)
        {
            item.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            width = Math.Max(width, item.DesiredSize.Width); height = Math.Max(height, item.DesiredSize.Height);
        }
        CellSize = _realized.Count == 0 ? 48 : _realized.Values.Max(p => ((MaterialCalendarMonthView)p.Children[1]).CellSize);
        if (height > 0 && height != ItemHeight)
        {
            var oldHeight = ItemHeight; ItemHeight = height;
            _offset = _offset / oldHeight * height;
            ItemHeightChanged?.Invoke(oldHeight, height); Realize();
        }
        return new(width, Count * ItemHeight);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var item in _realized) item.Value.Arrange(new Rect(0, item.Key * ItemHeight, finalSize.Width, ItemHeight));
        return finalSize;
    }
}
