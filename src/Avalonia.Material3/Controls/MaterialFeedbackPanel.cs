using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

/// <summary>Adaptive message/action/dismiss layout. Template children: message, action, dismiss.</summary>
public class MaterialFeedbackPanel : Panel
{
    public static readonly StyledProperty<bool> ActionOnNewLineProperty = AvaloniaProperty.Register<MaterialFeedbackPanel, bool>(nameof(ActionOnNewLine));
    public bool ActionOnNewLine { get => GetValue(ActionOnNewLineProperty); set => SetValue(ActionOnNewLineProperty, value); }
    private bool _stacked;
    private double _firstHeight, _secondHeight;
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 3) return base.MeasureOverride(availableSize);
        var message = Children[0]; var action = Children[1]; var dismiss = Children[2];
        dismiss.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        action.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var controls = action.DesiredSize.Width + dismiss.DesiredSize.Width;
        _stacked = ActionOnNewLine || double.IsFinite(availableSize.Width) && controls + 112 > availableSize.Width;
        action.Measure(new Size(Math.Max(0, availableSize.Width - dismiss.DesiredSize.Width), double.PositiveInfinity));
        message.Measure(new Size(Math.Max(0, availableSize.Width - dismiss.DesiredSize.Width - (_stacked ? 0 : action.DesiredSize.Width)), double.PositiveInfinity));
        _firstHeight = Math.Max(48, Math.Max(message.DesiredSize.Height, dismiss.DesiredSize.Height));
        _secondHeight = _stacked && action.IsVisible ? Math.Max(48, action.DesiredSize.Height) : 0;
        if (!_stacked) _firstHeight = Math.Max(_firstHeight, action.DesiredSize.Height);
        return new Size(Math.Min(availableSize.Width, message.DesiredSize.Width + controls), _firstHeight + _secondHeight);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 3) return base.ArrangeOverride(finalSize);
        var message = Children[0]; var action = Children[1]; var dismiss = Children[2];
        var dismissWidth = Math.Min(finalSize.Width, dismiss.DesiredSize.Width);
        var actionWidth = Math.Min(Math.Max(0, finalSize.Width - dismissWidth), action.DesiredSize.Width);
        message.Arrange(new Rect(0, 0, Math.Max(0, finalSize.Width - dismissWidth - (_stacked ? 0 : actionWidth)), _firstHeight));
        dismiss.Arrange(new Rect(Math.Max(0, finalSize.Width - dismissWidth), 0, dismissWidth, _firstHeight));
        action.Arrange(new Rect(_stacked ? 0 : Math.Max(0, finalSize.Width - dismissWidth - actionWidth),
            _stacked ? _firstHeight : 0, _stacked ? finalSize.Width : actionWidth, _stacked ? _secondHeight : _firstHeight));
        return finalSize;
    }
}
