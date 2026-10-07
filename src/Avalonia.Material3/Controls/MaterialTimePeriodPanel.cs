using Avalonia.Controls;
using Avalonia.Layout;

namespace Avalonia.Material3.Controls;

// AndroidX updated TimePicker toggle: disconnected faces,4-DIP gap,52×80 or216×38.
// Each38-DIP face retains a centered48-DIP native target.
internal sealed class MaterialTimePeriodPanel : Panel
{
    private readonly MaterialTimePeriodButton _am, _pm;
    private int _columns = 1;
    public int Columns
    {
        get => _columns;
        set { if (_columns == value) return; _columns = value; InvalidateMeasure(); }
    }
    public MaterialTimePeriodPanel(MaterialTimePeriodButton am, MaterialTimePeriodButton pm)
    {
        _am = am; _pm = pm;
        foreach (var button in new[] { am, pm })
        {
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.VerticalAlignment = VerticalAlignment.Stretch;
            Children.Add(button);
        }
    }
    private double FaceHeight => Math.Max(38, Math.Max(_am.FontSize, _pm.FontSize) * 1.5);
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = Columns == 2 ? Math.Max(0, (availableSize.Width - 4) / 2) : availableSize.Width;
        _am.Measure(new Size(width, double.PositiveInfinity)); _pm.Measure(new Size(width, double.PositiveInfinity));
        return Columns == 2 ? new(_am.DesiredSize.Width + _pm.DesiredSize.Width + 4, FaceHeight)
            : new(Math.Max(_am.DesiredSize.Width, _pm.DesiredSize.Width), FaceHeight * 2 + 4);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var faceHeight = Columns == 2 ? finalSize.Height : (finalSize.Height - 4) / 2;
        var targetHeight = Math.Max(48, faceHeight);
        var targetOffset = (faceHeight - targetHeight) / 2;
        if (Columns == 2)
        {
            var width = Math.Max(0, (finalSize.Width - 4) / 2);
            _am.Arrange(new Rect(0, targetOffset, width, targetHeight));
            _pm.Arrange(new Rect(width + 4, targetOffset, width, targetHeight));
        }
        else
        {
            _am.Arrange(new Rect(0, targetOffset, finalSize.Width, targetHeight));
            _pm.Arrange(new Rect(0, faceHeight + 4 + targetOffset, finalSize.Width, targetHeight));
        }
        return finalSize;
    }
}
