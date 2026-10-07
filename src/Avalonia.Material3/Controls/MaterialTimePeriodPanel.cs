using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

// Standard generated tertiary-token recipe (not Compose's flag-enabled expressive toggle).
// The visual envelope deliberately expands to two disjoint48-DIP native targets.
internal sealed class MaterialTimePeriodPanel : Panel
{
    private readonly MaterialTimePeriodButton _am, _pm;
    private readonly Outline _outline;
    private int _columns = 1;
    private static readonly StyledProperty<CornerRadius> RadiusProperty =
        Border.CornerRadiusProperty.AddOwner<MaterialTimePeriodPanel>();
    public int Columns
    {
        get => _columns;
        set { if (_columns == value) return; _columns = value; UpdateCorners(); InvalidateMeasure(); _outline.InvalidateVisual(); }
    }
    public MaterialTimePeriodPanel(MaterialTimePeriodButton am, MaterialTimePeriodButton pm)
    {
        _am = am; _pm = pm; _outline = new Outline(this) { IsHitTestVisible = false };
        foreach (var button in new[] { am, pm })
        {
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.VerticalAlignment = VerticalAlignment.Stretch;
            Children.Add(button);
        }
        Children.Add(_outline);
        MaterialPickerSupport.Resource(this, RadiusProperty, "Shape.CornerSmall");
        MaterialPickerSupport.Resource(_outline, TextBlock.ForegroundProperty, "OutlineBrush");
        UpdateCorners();
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == RadiusProperty && _am is not null) { UpdateCorners(); _outline.InvalidateVisual(); }
    }
    private void UpdateCorners()
    {
        var radius = GetValue(RadiusProperty).TopLeft;
        _am.CornerRadius = Columns == 2 ? new(radius, 0, 0, radius) : new(radius, radius, 0, 0);
        _pm.CornerRadius = Columns == 2 ? new(0, radius, radius, 0) : new(0, 0, radius, radius);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = Columns == 2 ? availableSize.Width / 2 : availableSize.Width;
        _am.Measure(new Size(width, double.PositiveInfinity)); _pm.Measure(new Size(width, double.PositiveInfinity));
        return Columns == 2 ? new(_am.DesiredSize.Width + _pm.DesiredSize.Width, Math.Max(_am.DesiredSize.Height, _pm.DesiredSize.Height))
            : new(Math.Max(_am.DesiredSize.Width, _pm.DesiredSize.Width), _am.DesiredSize.Height + _pm.DesiredSize.Height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Columns == 2)
        {
            _am.Arrange(new Rect(0, 0, finalSize.Width / 2, finalSize.Height));
            _pm.Arrange(new Rect(finalSize.Width / 2, 0, finalSize.Width / 2, finalSize.Height));
        }
        else
        {
            _am.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height / 2));
            _pm.Arrange(new Rect(0, finalSize.Height / 2, finalSize.Width, finalSize.Height / 2));
        }
        _outline.Arrange(new Rect(finalSize));
        return finalSize;
    }
    private sealed class Outline(MaterialTimePeriodPanel owner) : Control
    {
        public override void Render(DrawingContext context)
        {
            var pen = new Pen(GetValue(TextBlock.ForegroundProperty), 1);
            var radius = Math.Max(0, owner.GetValue(RadiusProperty).TopLeft - .5);
            context.DrawRectangle(null, pen, new Rect(.5, .5, Math.Max(0, Bounds.Width - 1), Math.Max(0, Bounds.Height - 1)), radius, radius);
            if (owner.Columns == 2) context.DrawLine(pen, new Point(Bounds.Width / 2, 1), new Point(Bounds.Width / 2, Bounds.Height - 1));
            else context.DrawLine(pen, new Point(1, Bounds.Height / 2), new Point(Bounds.Width - 1, Bounds.Height / 2));
        }
    }
}
